using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Core.Http;
using BancoBr.API.Santander.Errors;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Pix
{
    /// <summary>
    /// Cliente para a API "Pix" do Hub de Pagamentos do Santander (management_payments_partners
    /// v1) — transferência por chave DICT e pagamento de QR Code. Todo pagamento tem dois
    /// passos: POST (iniciação, valida a chave/QR e devolve READY_TO_PAY) e PATCH (efetivação,
    /// enviando AUTHORIZED). Transferência por agência e conta e webhook não são implementados.
    ///
    /// Toda chamada acontece dentro de uma workspace do Hub de Pagamentos. O cliente a resolve
    /// sozinho pela conta pagadora (<c>origem</c>): usa a workspace PAYMENTS ativa que contém a
    /// conta, habilita o Pix nela se estiver desligado e cria uma nova se não existir — o ERP
    /// nunca lida com workspace.
    ///
    /// Assim como no Sicoob, esta classe é o limite de mapeamento entre o contrato público
    /// (<see cref="Movimento"/>/<see cref="MovimentoItem"/>) e os DTOs de wire
    /// (<c>Pix.Models.*</c>). O identificador do pagamento no banco (UUID do POST) é guardado
    /// em <see cref="Movimento.NumeroDocumentoNoBanco"/> — o EndToEndId só existe depois da
    /// efetivação e não é exposto.
    /// </summary>
    internal class PagamentoPixClient : PagamentoPixApiBase
    {
        public static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/management_payments_partners/v1");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://trust-open.api.santander.com.br/auth/oauth/v2/token");

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private readonly Dictionary<string, (string WorkspaceId, ContaDebito Conta)> _workspaces = new Dictionary<string, (string WorkspaceId, ContaDebito Conta)>();
        private readonly SemaphoreSlim _workspacesLock = new SemaphoreSlim(1, 1);
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoPixClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        internal PagamentoPixClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public PagamentoPixClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        private static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            return new HttpClient(certHandler);
        }

        private static OAuthTokenProvider BuildTokenProvider(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint)
        {
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            return new OAuthTokenProvider(BuildHttpClient(certificateSource), tokenOptions);
        }

        #region ::. Operações .::

        internal override async Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar um pagamento Pix, o movimento precisa ter o id do pagamento em NumeroDocumentoNoBanco.");

            var workspace = await ResolverWorkspaceAsync(origem, cancellationToken).ConfigureAwait(false);
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/pix_payments/{movimento.NumeroDocumentoNoBanco}";
            var dto = await SendAsync<PagamentoPixResponse>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, dto);
        }

        internal override async Task<Movimento> IniciarPagamentoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
            if (item == null)
                throw new InvalidOperationException("IniciarPagamentoAsync espera um MovimentoItemTransferenciaPIX em Movimento.MovimentoItem.");

            var wireRequest = new RequisicaoPagamentoPix
            {
                RemittanceInformation = movimento.NumeroDocumento,
                PaymentValue = movimento.ValorPagamento,
            };

            // Sem chave Pix, a transferência é para a agência e conta do beneficiário.
            if (string.IsNullOrWhiteSpace(item.ChavePIX))
            {
                wireRequest.Beneficiary = MontarBeneficiario(movimento, item);
            }
            else
            {
                wireRequest.DictCodeType = MapTipoChave(item);
                wireRequest.DictCode = item.ChavePIX;
            }

            return await IniciarAsync(movimento, origem, wireRequest, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para confirmar um pagamento Pix, o movimento precisa ter o id do pagamento (devolvido por IniciarPagamentoAsync) em NumeroDocumentoNoBanco.");

            var workspace = await ResolverWorkspaceAsync(origem, cancellationToken).ConfigureAwait(false);
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/pix_payments/{movimento.NumeroDocumentoNoBanco}";

            // A conta é enviada explicitamente: se a workspace foi achada por uma conta adicional,
            // sem isso o banco debitaria a conta principal.
            var json = JsonConvert.SerializeObject(new RequisicaoConfirmacaoPix { PaymentValue = movimento.ValorPagamento, DebitAccount = workspace.Conta }, SerializerSettings);

            var dto = await SendAsync<PagamentoPixResponse>(new HttpMethod("PATCH"), url, json, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, dto);
        }

        /// <summary>
        /// Diferente do Sicoob, o QR Code também passa por iniciação + efetivação: o POST valida o
        /// QR e devolve READY_TO_PAY, e o PATCH efetiva.
        /// </summary>
        internal override async Task<Movimento> PagarViaQrCodeAsync(Correntista origem, Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoTituloPIXQRCode;
            if (item == null)
                throw new InvalidOperationException("PagarViaQrCodeAsync espera um MovimentoItemPagamentoTituloPIXQRCode em Movimento.MovimentoItem.");

            var wireRequest = new RequisicaoPagamentoPix
            {
                RemittanceInformation = movimento.NumeroDocumento,
                QrCode = item.QrCode,
                PaymentValue = movimento.ValorPagamento,
                PaymentDate = movimento.DataPagamento.Date > DateTime.Today ? movimento.DataPagamento : (DateTime?)null,
            };

            await IniciarAsync(movimento, origem, wireRequest, cancellationToken).ConfigureAwait(false);
            if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            return await ConfirmarPagamentoAsync(movimento, origem, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<Movimento> PagarComIniciacaoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            await IniciarPagamentoAsync(movimento, origem, cancellationToken).ConfigureAwait(false);
            if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            return await ConfirmarPagamentoAsync(movimento, origem, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Movimento> IniciarAsync(Movimento movimento, Correntista origem, RequisicaoPagamentoPix wireRequest, CancellationToken cancellationToken)
        {
            var workspace = await ResolverWorkspaceAsync(origem, cancellationToken).ConfigureAwait(false);
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/pix_payments";
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            var dto = await SendAsync<PagamentoPixResponse>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, dto);
        }

        #endregion

        #region ::. Workspace .::

        /// <summary>
        /// Acha a workspace PAYMENTS ativa que contém a conta pagadora (principal ou adicional).
        /// Se existir com o Pix desligado, liga; se não existir, cria. O resultado fica em cache
        /// por conta, e a trava evita criar duas workspaces em chamadas concorrentes.
        /// </summary>
        private async Task<(string WorkspaceId, ContaDebito Conta)> ResolverWorkspaceAsync(Correntista origem, CancellationToken cancellationToken)
        {
            if (origem == null)
                throw new ArgumentNullException(nameof(origem), "O Santander exige a conta pagadora (origem) para localizar a workspace do Pix.");

            var alvo = new ContaDebito
            {
                Branch = origem.NumeroAgencia.ToString("D4"),
                Number = $"{origem.NumeroConta}{origem.DVConta}".ToUpperInvariant().PadLeft(12, '0'),
            };
            var chave = $"{NormalizarConta(alvo.Branch)}/{NormalizarConta(alvo.Number)}";

            await _workspacesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_workspaces.TryGetValue(chave, out var emCache))
                    return emCache;

                var existentes = await ListarWorkspacesAsync(cancellationToken).ConfigureAwait(false);
                var workspace = existentes
                    .Where(w => w.Status == "ACTIVE" && w.Type == "PAYMENTS" && ContaDaWorkspace(w, alvo) != null)
                    .OrderByDescending(w => w.PixPaymentsActive)
                    .FirstOrDefault();

                (string WorkspaceId, ContaDebito Conta) resolvida;
                if (workspace == null)
                {
                    var nova = new Workspace { Type = "PAYMENTS", Description = "BancoBr Pix", MainDebitAccount = alvo, PixPaymentsActive = true };
                    var criada = await SendAsync<Workspace>(HttpMethod.Post, $"{_baseUrl}workspaces", JsonConvert.SerializeObject(nova, SerializerSettings), cancellationToken).ConfigureAwait(false);
                    resolvida = (criada.IdResolvido, alvo);
                }
                else
                {
                    var conta = ContaDaWorkspace(workspace, alvo);
                    var id = workspace.IdResolvido;

                    if (!workspace.PixPaymentsActive)
                    {
                        // PATCH reenvia a workspace como veio do GET, mudando só a flag do Pix.
                        workspace.Id = workspace.WorkspaceId = workspace.Status = workspace.Type = null;
                        workspace.AdditionalDebitAccounts = workspace.AdditionalDebitAccounts ?? workspace.AddicionalDebitAccounts;
                        workspace.AddicionalDebitAccounts = null;
                        workspace.PixPaymentsActive = true;
                        await SendAsync<Workspace>(new HttpMethod("PATCH"), $"{_baseUrl}workspaces/{id}", JsonConvert.SerializeObject(workspace, SerializerSettings), cancellationToken).ConfigureAwait(false);
                    }

                    resolvida = (id, conta);
                }

                _workspaces[chave] = resolvida;
                return resolvida;
            }
            finally
            {
                _workspacesLock.Release();
            }
        }

        private async Task<List<Workspace>> ListarWorkspacesAsync(CancellationToken cancellationToken)
        {
            const int limite = 50;
            var todas = new List<Workspace>();

            while (true)
            {
                var dto = await SendAsync<WorkspaceListResponse>(HttpMethod.Get, $"{_baseUrl}workspaces?_limit={limite}&_offset={todas.Count}", body: null, cancellationToken).ConfigureAwait(false);
                if (dto?.Content == null || dto.Content.Count == 0)
                    break;

                todas.AddRange(dto.Content);
                if (dto.Pageable == null || todas.Count >= dto.Pageable.TotalElements)
                    break;
            }

            return todas;
        }

        /// <summary>Devolve a conta da workspace que corresponde a <paramref name="alvo"/> (do jeito que o banco a grafou), ou nulo.</summary>
        private static ContaDebito ContaDaWorkspace(Workspace workspace, ContaDebito alvo) =>
            new[] { workspace.MainDebitAccount }
                .Concat(workspace.AdditionalDebitAccounts ?? workspace.AddicionalDebitAccounts ?? new List<ContaDebito>())
                .FirstOrDefault(c => c != null && NormalizarConta(c.Branch) == NormalizarConta(alvo.Branch) && NormalizarConta(c.Number) == NormalizarConta(alvo.Number));

        /// <summary>Ignora zeros à esquerda e caixa, já que o banco devolve agência/conta preenchidas até o tamanho máximo.</summary>
        private static string NormalizarConta(string valor) => (valor ?? string.Empty).Trim().TrimStart('0').ToUpperInvariant();

        #endregion

        #region ::. Mapeamento de resposta .::

        private static string MapTipoChave(MovimentoItemTransferenciaPIX item)
        {
            switch (item.TipoChavePIX)
            {
                case FormaIniciacaoEnum.PIX_Telefone:
                    return "CELULAR";

                case FormaIniciacaoEnum.PIX_Email:
                    return "EMAIL";

                case FormaIniciacaoEnum.PIX_Aleatorio:
                    return "EVP";

                case FormaIniciacaoEnum.PIX_CPF_CNPJ:
                    return new string(item.ChavePIX.Where(char.IsDigit).ToArray()).Length > 11 ? "CNPJ" : "CPF";

                default:
                    throw new InvalidOperationException("O tipo da chave Pix (TipoChavePIX) não foi informado no movimento.");
            }
        }

        private static PixBeneficiario MontarBeneficiario(Movimento movimento, MovimentoItemTransferenciaPIX item)
        {
            if (item.NumeroAgencia == 0 || item.NumeroConta == 0)
                throw new InvalidOperationException("Informe a chave Pix, ou a agência e a conta do beneficiário, no movimento.");

            if (movimento.Favorecido == null || string.IsNullOrWhiteSpace(movimento.Favorecido.Nome) || string.IsNullOrWhiteSpace(movimento.Favorecido.CPF_CNPJ))
                throw new InvalidOperationException("O Pix por agência e conta exige o nome e o CPF/CNPJ do favorecido (Movimento.Favorecido).");

            var informouBanco = item.Banco != 0;
            var informouIspb = !string.IsNullOrWhiteSpace(item.Ispb);
            if (informouBanco == informouIspb)
                throw new InvalidOperationException("Informe o banco do beneficiário por código (Banco) ou por ISPB (Ispb) — um dos dois, nunca ambos.");

            string tipoConta;
            switch (item.TipoConta)
            {
                case TipoContaEnum.ContaPoupanca:
                    tipoConta = "CONTA_POUPANCA";
                    break;

                case TipoContaEnum.ContaPagamento:
                    tipoConta = "CONTA_PAGAMENTO";
                    break;

                default:
                    tipoConta = "CONTA_CORRENTE";
                    break;
            }

            return new PixBeneficiario
            {
                Name = movimento.Favorecido.Nome,
                DocumentType = movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ ? "CNPJ" : "CPF",
                DocumentNumber = movimento.Favorecido.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                BankCode = informouBanco ? item.Banco.ToString("D3") : null,
                Ispb = informouIspb ? item.Ispb : null,
                Branch = item.NumeroAgencia.ToString("D4"),
                Number = $"{item.NumeroConta}{item.DVConta}",
                Type = tipoConta,
            };
        }

        private static Movimento AplicarResposta(Movimento movimento, PagamentoPixResponse dto)
        {
            if (dto == null)
                return movimento;

            if (!string.IsNullOrWhiteSpace(dto.Id))
                movimento.NumeroDocumentoNoBanco = dto.Id;

            movimento.SituacaoBancoBr = MapStatusParaSituacao(dto.Status);
            movimento.DetalheRejeicaoBancoBr = dto.RejectReason;
            movimento.HorarioBancoBr = dto.Transaction?.Date;

            if (dto.PaymentValue != 0)
                movimento.ValorPagamento = dto.PaymentValue;

            var itemPix = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
            if (itemPix != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.DictCode))
                    itemPix.ChavePIX = dto.DictCode;

                if (!string.IsNullOrWhiteSpace(dto.DictCodeType))
                    itemPix.TipoChaveConfirmado = dto.DictCodeType;

                if (dto.Beneficiary != null)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Beneficiary.Name))
                        itemPix.TitularConfirmadoNome = dto.Beneficiary.Name;

                    if (!string.IsNullOrWhiteSpace(dto.Beneficiary.DocumentNumber))
                        itemPix.TitularConfirmadoDocumento = dto.Beneficiary.DocumentNumber;

                    if (!string.IsNullOrWhiteSpace(dto.Beneficiary.DocumentType))
                        itemPix.TitularConfirmadoTipo = dto.Beneficiary.DocumentType;
                }
            }

            var itemQrCode = movimento.MovimentoItem as MovimentoItemPagamentoTituloPIXQRCode;
            if (itemQrCode != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.QrCodeIdentification))
                    itemQrCode.TXID = dto.QrCodeIdentification;

                if (dto.NominalValue != 0)
                    itemQrCode.ValorOriginal = dto.NominalValue;

                if (dto.DueDate.HasValue)
                    itemQrCode.DataVencimento = dto.DueDate.Value;
            }

            return movimento;
        }

        /// <summary>
        /// READY_TO_PAY (iniciado, aguardando o PATCH) e PENDING_VALIDATION ainda não são um
        /// pagamento em andamento, por isso ficam em NaoIntegrado — assim como qualquer valor
        /// não reconhecido, para nunca reportar falsamente Efetivado/Cancelado.
        /// PENDING_CONFIRMATION (câmara fora do ar, banco recebedor avaliando, falta de saldo...)
        /// e SCHEDULED seguem como Agendado: o resultado final sai de uma nova consulta.
        /// </summary>
        private static BancoBrSituacaoEnum MapStatusParaSituacao(string status)
        {
            switch (status?.Trim().ToUpperInvariant())
            {
                case "PAYED":
                    return BancoBrSituacaoEnum.Efetivado;

                case "PENDING_CONFIRMATION":
                case "SCHEDULED":
                    return BancoBrSituacaoEnum.Agendado;

                case "REJECTED":
                    return BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        #endregion

        #region ::. Plumbing HTTP .::

        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                EnsureSuccessOrThrow(response, responseBody);

                return JsonConvert.DeserializeObject<T>(responseBody, SerializerSettings);
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Application-Key", _clientId);

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private async Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var response = await SendOnceAsync(requestFactory, token, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                _tokenProvider.InvalidateToken();
                token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                response = await SendOnceAsync(requestFactory, token, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> requestFactory, string token, CancellationToken cancellationToken)
        {
            using (var request = requestFactory())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Todos os status de erro do Hub de Pagamentos seguem o mesmo corpo
        /// (_errorCode/_message/_details/_errors[]). Se o corpo não vier nesse formato, cai para
        /// o texto bruto da resposta, para nunca esconder a causa real de uma falha.
        /// </summary>
        private static void EnsureSuccessOrThrow(HttpResponseMessage response, string body)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var mensagens = new List<MensagemErro>();

            SantanderErrorResponse erro;
            try
            {
                erro = JsonConvert.DeserializeObject<SantanderErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                erro = null;
            }

            if (erro != null && (!string.IsNullOrEmpty(erro.Message) || !string.IsNullOrEmpty(erro.Details)))
            {
                mensagens.Add(new MensagemErro { Codigo = erro.ErrorCode, Mensagem = string.IsNullOrEmpty(erro.Details) ? erro.Message : erro.Details });
            }

            if (erro?.Errors != null)
            {
                foreach (var item in erro.Errors)
                {
                    mensagens.Add(new MensagemErro { Codigo = item.Code, Mensagem = string.IsNullOrEmpty(item.Field) ? item.Message : $"{item.Field}: {item.Message}" });
                }
            }

            if (mensagens.Count == 0 && !string.IsNullOrWhiteSpace(body))
            {
                mensagens.Add(new MensagemErro { Codigo = ((int)response.StatusCode).ToString(), Mensagem = body });
            }

            throw new BancoApiException((int)response.StatusCode, mensagens);
        }

        #endregion
    }
}

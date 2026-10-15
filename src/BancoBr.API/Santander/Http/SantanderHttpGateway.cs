using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Errors;
using BancoBr.API.Santander.Pagamentos.Models;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Http
{
    /// <summary>
    /// Plumbing HTTP comum às APIs do Santander: mTLS, OAuth2 com renovação automática do token
    /// no 401, header <c>X-Application-Key</c> e normalização do corpo de erro em
    /// <see cref="BancoApiException"/>, mais a obtenção/criação de workspace do Hub de Pagamentos e a
    /// normalização de status, comuns a Pix, Boleto, Convênio e TED. Usado por composição (os clientes já herdam de uma
    /// <c>*ApiBase</c>, então não dá para herdar daqui).
    /// </summary>
    internal class SantanderHttpGateway
    {
        public static readonly Uri DefaultTokenEndpoint = new Uri("https://trust-open.api.santander.com.br/auth/oauth/v2/token");

        /// <summary>Base da API do Hub de Pagamentos (management_payments_partners v1), igual para Pix, boleto e convênio.</summary>
        public static readonly Uri PagamentosBaseUrl = new Uri("https://trust-open.api.santander.com.br/management_payments_partners/v1");

        /// <summary>Base da API de consulta de extrato (bank_account_information v1).</summary>
        public static readonly Uri ContaCorrenteBaseUrl = new Uri("https://trust-open.api.santander.com.br/bank_account_information/v1");

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        private readonly HttpClient _httpClient;
        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly SemaphoreSlim _workspacesLock = new SemaphoreSlim(1, 1);

        public SantanderHttpGateway(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        }

        public static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            return new HttpClient(certHandler);
        }

        public static OAuthTokenProvider BuildTokenProvider(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint)
        {
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = tokenEndpoint ?? DefaultTokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
            };

            return new OAuthTokenProvider(BuildHttpClient(certificateSource), tokenOptions);
        }

        public async Task<T> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken cancellationToken)
        {
            var json = body == null ? null : JsonConvert.SerializeObject(body, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, json), cancellationToken).ConfigureAwait(false))
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

        #region ::. Pagamento por código (boleto e código de barras) .::

        /// <summary>
        /// Os dois produtos têm o mesmo ciclo: POST inicia (READY_TO_PAY), PATCH efetiva e GET
        /// consulta, sempre no mesmo recurso — <c>bank_slip_payments</c> (boleto) ou
        /// <c>barcode_payments</c> (convênio/arrecadação).
        /// </summary>
        public Task<PagamentoCodigoResponse> IniciarPagamentoCodigoAsync(string baseUrl, string recurso, string workspaceId, string codigo, DateTime dataPagamento, CancellationToken cancellationToken)
        {
            var corpo = new RequisicaoPagamentoCodigo
            {
                Code = codigo,
                PaymentDate = dataPagamento.Date > DateTime.Today ? dataPagamento : (DateTime?)null,
            };

            return SendAsync<PagamentoCodigoResponse>(HttpMethod.Post, $"{baseUrl}workspaces/{workspaceId}/{recurso}", corpo, cancellationToken);
        }

        /// <summary>
        /// O pagador final é obrigatório e, numa workspace PAYMENTS, tem de repetir o titular da
        /// workspace — ou seja, a própria conta pagadora (<paramref name="origem"/>). A conta é
        /// enviada explicitamente: se a workspace foi achada por uma conta adicional, sem isso o
        /// banco debitaria a conta principal.
        /// </summary>
        public Task<PagamentoCodigoResponse> EfetivarPagamentoCodigoAsync(string baseUrl, string recurso, (string WorkspaceId, ContaDebito Conta) workspace, string pagamentoId, decimal valor, Correntista origem, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(origem.Nome) || string.IsNullOrWhiteSpace(origem.CPF_CNPJ))
                throw new InvalidOperationException("O Santander exige o pagador final: informe o nome e o CPF/CNPJ do titular da conta pagadora (origem).");

            var confirmacao = new RequisicaoConfirmacaoPix
            {
                PaymentValue = valor,
                DebitAccount = workspace.Conta,
                FinalPayer = new PixBeneficiario
                {
                    Name = origem.Nome,
                    DocumentType = origem.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ ? "CNPJ" : "CPF",
                    DocumentNumber = origem.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                },
            };

            return SendAsync<PagamentoCodigoResponse>(new HttpMethod("PATCH"), $"{baseUrl}workspaces/{workspace.WorkspaceId}/{recurso}/{pagamentoId}", confirmacao, cancellationToken);
        }

        /// <summary>
        /// Aplica no movimento o que é comum às respostas de boleto e código de barras: situação,
        /// motivo de rejeição, horário da transação e o valor a pagar. O valor do banco
        /// (<c>paymentValue</c>, ou o <c>totalValue</c> calculado) só substitui o do movimento
        /// quando o ERP ainda não informou um.
        /// </summary>
        public static void AplicarStatusCodigo(Movimento movimento, PagamentoCodigoResponse dto)
        {
            movimento.SituacaoBancoBr = MapStatusParaSituacao(dto.Status);
            movimento.DetalheRejeicaoBancoBr = dto.RejectReason;
            movimento.HorarioBancoBr = dto.Transaction?.Date;

            if (dto.PaymentValue.GetValueOrDefault() != 0)
                movimento.ValorPagamento = dto.PaymentValue.Value;
            else if (movimento.ValorPagamento == 0 && dto.TotalValue.GetValueOrDefault() != 0)
                movimento.ValorPagamento = dto.TotalValue.Value;
        }

        public Task<PagamentoCodigoResponse> ConsultarPagamentoCodigoAsync(string baseUrl, string recurso, string workspaceId, string pagamentoId, CancellationToken cancellationToken) =>
            SendAsync<PagamentoCodigoResponse>(HttpMethod.Get, $"{baseUrl}workspaces/{workspaceId}/{recurso}/{pagamentoId}", body: null, cancellationToken);

        #endregion

        #region ::. Workspace do Hub de Pagamentos .::

        /// <summary>
        /// Workspace informada pelo chamador em <c>BancoApi.Conectar</c>, com a conta de débito
        /// derivada da conta pagadora (<paramref name="origem"/>). Não consulta o banco: a
        /// workspace é obtida uma única vez por <see cref="ObterOuCriarWorkspaceAsync"/> e o id é
        /// guardado pelo ERP.
        /// </summary>
        public static (string WorkspaceId, ContaDebito Conta) UsarWorkspace(string idWorkspace, Correntista origem, string produto)
        {
            if (origem == null)
                throw new ArgumentNullException(nameof(origem), $"O Santander exige a conta pagadora (origem) para operar {produto}.");

            if (string.IsNullOrWhiteSpace(idWorkspace))
                throw new InvalidOperationException($"O Santander exige o id da workspace para operar {produto}: obtenha-o com BancoApi.ObterOuCriarWorkspaceAsync e informe em BancoApi.Conectar (idWorkspace).");

            return (idWorkspace, ContaDebitoDe(origem));
        }

        /// <summary>Conta pagadora no formato do Hub de Pagamentos: agência com 4 dígitos e conta com DV em 12.</summary>
        private static ContaDebito ContaDebitoDe(Correntista origem) => new ContaDebito
        {
            Branch = origem.NumeroAgencia.ToString("D4"),
            Number = $"{origem.NumeroConta}{origem.DVConta}".ToUpperInvariant().PadLeft(12, '0'),
        };

        /// <summary>
        /// Acha a workspace PAYMENTS ativa que contém a conta pagadora (principal ou adicional)
        /// e devolve o seu id. Se existir com algum produto desligado, liga todos; se não
        /// existir, cria já com todos ligados. Havendo mais de uma, prefere a que já tem mais
        /// produtos ligados. A trava evita criar duas workspaces em chamadas concorrentes.
        /// </summary>
        public async Task<string> ObterOuCriarWorkspaceAsync(string baseUrl, Correntista origem, CancellationToken cancellationToken)
        {
            if (origem == null)
                throw new ArgumentNullException(nameof(origem), "O Santander exige a conta pagadora (origem) para localizar a workspace.");

            var alvo = ContaDebitoDe(origem);

            await _workspacesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var existentes = await ListarWorkspacesAsync(baseUrl, cancellationToken).ConfigureAwait(false);
                var workspace = existentes
                    .Where(w => w.Status == "ACTIVE" && w.Type == "PAYMENTS" && ContaDaWorkspace(w, alvo) != null)
                    .OrderByDescending(ProdutosAtivos)
                    .FirstOrDefault();

                if (workspace == null)
                {
                    var nova = new Workspace { Type = "PAYMENTS", Description = "BancoBr", MainDebitAccount = alvo };
                    AtivarProdutos(nova);
                    var criada = await SendAsync<Workspace>(HttpMethod.Post, $"{baseUrl}workspaces", nova, cancellationToken).ConfigureAwait(false);
                    return criada.IdResolvido;
                }

                var id = workspace.IdResolvido;

                if (ProdutosAtivos(workspace) < TotalProdutos)
                {
                    // PATCH reenvia a workspace como veio do GET, mudando só as flags dos produtos.
                    workspace.Id = workspace.WorkspaceId = workspace.Status = workspace.Type = null;
                    workspace.AdditionalDebitAccounts = workspace.AdditionalDebitAccounts ?? workspace.AddicionalDebitAccounts;
                    workspace.AddicionalDebitAccounts = null;
                    AtivarProdutos(workspace);
                    await SendAsync<Workspace>(new HttpMethod("PATCH"), $"{baseUrl}workspaces/{id}", workspace, cancellationToken).ConfigureAwait(false);
                }

                return id;
            }
            finally
            {
                _workspacesLock.Release();
            }
        }

        /// <summary>Pix, boleto, DDA, código de barras (convênio) e TED — os produtos usados pela BancoBr.</summary>
        private const int TotalProdutos = 5;

        private static int ProdutosAtivos(Workspace w) =>
            new[] { w.PixPaymentsActive, w.BankSlipPaymentsActive, w.BankSlipAvailableActive, w.BarCodePaymentsActive, w.BankTransferPaymentsActive }.Count(ativo => ativo);

        private static void AtivarProdutos(Workspace w) =>
            w.PixPaymentsActive = w.BankSlipPaymentsActive = w.BankSlipAvailableActive = w.BarCodePaymentsActive = w.BankTransferPaymentsActive = true;

        private async Task<List<Workspace>> ListarWorkspacesAsync(string baseUrl, CancellationToken cancellationToken)
        {
            const int limite = 50;
            var todas = new List<Workspace>();

            while (true)
            {
                var dto = await SendAsync<WorkspaceListResponse>(HttpMethod.Get, $"{baseUrl}workspaces?_limit={limite}&_offset={todas.Count}", body: null, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// READY_TO_PAY (iniciado, aguardando o PATCH) e PENDING_VALIDATION ainda não são um
        /// pagamento em andamento, por isso ficam em NaoIntegrado — assim como qualquer valor
        /// não reconhecido, para nunca reportar falsamente Efetivado/Cancelado.
        /// PENDING_CONFIRMATION (câmara fora do ar, banco recebedor avaliando, falta de saldo...)
        /// e SCHEDULED seguem como Agendado: o resultado final sai de uma nova consulta.
        /// REFUNDED (boleto devolvido) é tratado como Cancelado.
        /// </summary>
        public static BancoBrSituacaoEnum MapStatusParaSituacao(string status)
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

                case "REFUNDED":
                    return BancoBrSituacaoEnum.Cancelado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
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
                return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Os erros seguem o corpo <c>_errorCode/_message/_details/_errors[]</c>. Se o corpo não
        /// vier nesse formato, cai para o texto bruto da resposta, para nunca esconder a causa
        /// real de uma falha.
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
    }
}

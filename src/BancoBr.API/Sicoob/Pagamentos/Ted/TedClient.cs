using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using BancoBr.API.Sicoob.Pagamentos.Ted.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted
{
    /// <summary>
    /// Cliente para a API "SPB Transferências" do Sicoob, v2 — envio de TED entre clientes.
    ///
    /// Esta classe é o limite de mapeamento entre o contrato público
    /// (<see cref="Movimento"/>/<see cref="MovimentoItemTransferenciaTED"/>, compartilhado com
    /// o CNAB) e os DTOs de wire do Sicoob (<c>Ted.Models.*</c>, detalhe de implementação) —
    /// mesmo papel que BancoBr.CNAB.Base.Banco tem para os Segmentos, e igualmente por
    /// composição, nunca por herança.
    /// </summary>
    internal class TedClient : PagamentoTedApiBase
    {
        /// <summary>
        /// Base URL intrínseca a esta API específica do Sicoob (SPB Transferências v2) — não
        /// faz sentido como configuração vinda do consumidor (ERP).
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/spb/v2");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        /// <summary>
        /// Confirmado no portal de developers do Sicoob (app do cliente): os scopes aprovados
        /// para a API SPB Transferências são "spb_consulta" e "spb_escrita" — meus primeiros
        /// valores ("spb_transferencias_consulta"/"spb_transferencias_escrita", um chute
        /// seguindo o padrão de nomenclatura das demais APIs) foram rejeitados pelo Sicoob com
        /// "invalid_scope" ao gerar o token OAuth2.
        /// </summary>
        private static readonly string[] Scopes = { "spb_consulta", "spb_escrita" };

        /// <summary>ATENÇÃO: rate limit não documentado — usando o mesmo padrão conservador de Boletos/Convênios.</summary>
        private const int RequestsPerSecond = 2;

        /// <summary>
        /// ISPB do Sicoob (Banco Cooperativo do Brasil), usado como "debtorAccount.ispb" (conta
        /// de origem é sempre do próprio Sicoob). Mesmo valor usado pela Pix Pagamentos.
        /// </summary>
        private const string SicoobIspb = "54037916";

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal TedClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal TedClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public TedClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
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

            var rateLimiter = new RateLimitingHandler(RequestsPerSecond)
            {
                InnerHandler = certHandler,
            };

            return new HttpClient(rateLimiter);
        }

        private static OAuthTokenProvider BuildTokenProvider(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            var tokenHttpClient = new HttpClient(certHandler);
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scopes = Scopes,
            };

            return new OAuthTokenProvider(tokenHttpClient, tokenOptions);
        }

        #region ::. Operações .::

        internal override async Task<Movimento> PagarTedAsync(Movimento movimento, Correntista origem, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaTED;
            if (item == null)
                throw new InvalidOperationException("PagarTedAsync espera um MovimentoItemTransferenciaTED em Movimento.MovimentoItem.");

            var url = $"{_baseUrl}transferencias";
            var wireRequest = MontarRequisicao(movimento, item, origem);
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Post, url, json, idempotencyKey), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // ATENÇÃO: a documentação rotula esta resposta como "204 - TED enviada" mas
                    // mostra um corpo de exemplo completo (inconsistência do Swagger). Um 204
                    // real (sem corpo) só confirma o aceite do envio — os detalhes
                    // (numeroControleIF, idAgendamento etc.) precisam ser buscados depois via
                    // ConsultarTedAsync.
                    movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Agendado;
                    movimento.DetalheRejeicaoBancoBr = "TED enviada; consulte novamente para obter o comprovante.";
                    return movimento;
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var dto = JsonConvert.DeserializeObject<TedRetorno>(body, SerializerSettings);
                return AplicarRetorno(movimento, item, dto);
            }
        }

        internal override async Task<Movimento> ConsultarTedAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar uma TED, o movimento precisa ter o numeroControleIF em NumeroDocumentoNoBanco.");

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaTED;
            if (item == null)
                throw new InvalidOperationException("ConsultarTedAsync espera um MovimentoItemTransferenciaTED em Movimento.MovimentoItem.");

            var url = $"{_baseUrl}transferencias/{movimento.NumeroDocumentoNoBanco}";
            var dto = await SendEnvelopedListAsync<TedRetorno>(HttpMethod.Get, url, idempotencyKey: null, cancellationToken).ConfigureAwait(false);

            return AplicarRetorno(movimento, item, dto);
        }

        internal override async Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaTED;
            if (item == null)
                throw new InvalidOperationException("CancelarAgendamentoAsync espera um MovimentoItemTransferenciaTED em Movimento.MovimentoItem.");

            if (item.IdAgendamento == 0)
                throw new InvalidOperationException("Para cancelar a TED, o movimento precisa ter o idAgendamento (devolvido por PagarTedAsync) em MovimentoItemTransferenciaTED.IdAgendamento.");

            var url = $"{_baseUrl}transferencias/agendamentos/{item.IdAgendamento}";
            var resultado = await SendEnvelopedListAsync<TedCancelamentoResultado>(HttpMethod.Delete, url, idempotencyKey, cancellationToken).ConfigureAwait(false);

            movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado;
            movimento.DetalheRejeicaoBancoBr = resultado != null && !string.IsNullOrWhiteSpace(resultado.Mensagem)
                ? resultado.Mensagem
                : "Agendamento cancelado.";

            return movimento;
        }

        #endregion

        #region ::. Mapeamento Movimento <-> Sicoob .::

        private static RequisicaoTed MontarRequisicao(Movimento movimento, MovimentoItemTransferenciaTED item, Correntista origem)
        {
            if (movimento.Favorecido == null)
                throw new InvalidOperationException("A TED exige o Favorecido (nome e CPF/CNPJ) preenchido no movimento.");

            var dataPagamento = movimento.DataPagamento == default(DateTime) ? DateTime.Today : movimento.DataPagamento;

            return new RequisicaoTed
            {
                DebtorAccount = new Models.DebtorAccount
                {
                    // A API exige a agência com 4 dígitos, zero à esquerda
                    // (ERRO_TAMANHO_NUMEROAGENCIA quando enviada sem padding).
                    Issuer = origem.NumeroAgencia.ToString("D4"),
                    Number = $"{origem.NumeroConta}{origem.DVConta}",
                    // A conta de origem é sempre corrente — única modalidade habilitada para
                    // pagamento via API (mesma premissa já usada por Boleto/Pix nesta lib).
                    AccountType = ContaWire(TipoContaEnum.ContaCorrente),
                    PersonType = PessoaWire(origem.TipoPessoa),
                    Ispb = SicoobIspb,
                },
                CreditorAccount = new CreditorAccount
                {
                    Ispb = TabelaIspbPorCompe.ObterIspb(item.Banco),
                    Issuer = item.NumeroAgencia.ToString("D4"),
                    Number = $"{item.NumeroConta}{item.DVConta}",
                    AccountType = ContaWire(item.TipoConta),
                    PersonType = PessoaWire(movimento.Favorecido.TipoPessoa),
                },
                Creditor = new Creditor
                {
                    PersonType = PessoaWire(movimento.Favorecido.TipoPessoa),
                    CpfCnpj = movimento.Favorecido.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                    Name = movimento.Favorecido.Nome,
                },
                Date = dataPagamento.ToString("yyyy-MM-dd"),
                Amount = movimento.ValorPagamento.ToString("0.00", CultureInfo.InvariantCulture),
                // Sem zero à esquerda — apesar do exemplo da doc ("00000"), o Sicoob rejeita
                // a finalidade com padding; espera o número puro (ex.: "10", não "00010").
                Finalidade = ((int)item.CodigoFinalidadeTED).ToString(),
                // EXPERIMENTAL/TEMPORÁRIO: fixo em "123", só para teste isolado do
                // ERRO_TAMANHO_NUMEROAGENCIA. Reverter depois do teste.
                NumeroPa = "123",
                Historico = item.Historico,
            };
        }

        private static string ContaWire(TipoContaEnum tipo) => tipo == TipoContaEnum.ContaPoupanca ? "SVGS" : "CACC";

        private static string PessoaWire(TipoInscricaoCPFCNPJEnum tipo) => tipo == TipoInscricaoCPFCNPJEnum.CNPJ ? "LEGAL_PERSON" : "NATURAL_PERSON";

        private static Movimento AplicarRetorno(Movimento movimento, MovimentoItemTransferenciaTED item, TedRetorno dto)
        {
            if (dto == null)
                return movimento;

            if (!string.IsNullOrWhiteSpace(dto.NumeroControleIF))
                movimento.NumeroDocumentoNoBanco = dto.NumeroControleIF;

            movimento.SituacaoBancoBr = MapSituacaoParaSituacao(dto.Situacao);
            movimento.DetalheRejeicaoBancoBr = dto.MensagemErro;

            if (decimal.TryParse(dto.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) && valor != 0)
                movimento.ValorPagamento = valor;

            if (DateTime.TryParse(dto.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) && data != default(DateTime))
                movimento.DataPagamento = data;

            item.NumeroControleIF = dto.NumeroControleIF;
            item.IdAgendamento = dto.IdAgendamento;
            item.Agendado = dto.Agendamento;
            item.Situacao = dto.Situacao;
            item.CodigoSituacaoAgendamento = dto.CodigoSituacaoAgendamento;
            item.MensagemErro = dto.MensagemErro;
            item.TipoPessoaDebito = dto.TipoPessoaDebito;
            item.NomePessoaDebito = dto.NomePessoaDebito;
            item.NumeroCPFCNPJDebito = dto.NumeroCPFCNPJDebito;

            if (!string.IsNullOrWhiteSpace(dto.Historico))
                item.Historico = dto.Historico;

            if (!string.IsNullOrWhiteSpace(dto.NumeroPa))
                item.NumeroPa = dto.NumeroPa;

            if (int.TryParse(dto.NumeroBancoFavorecido, out var numeroBancoFavorecido))
                item.NumeroBancoFavorecido = numeroBancoFavorecido;

            return movimento;
        }

        /// <summary>
        /// ATENÇÃO: mapeamento best-effort do campo textual "situação" retornado pela API SPB
        /// Transferências do Sicoob para o enum agnóstico <see cref="BancoBrSituacaoEnum"/>.
        /// Nenhum valor real foi confirmado em testes/documentação disponível neste
        /// repositório — a lista abaixo foi inferida do vocabulário já usado pelas outras APIs
        /// do Sicoob (Boleto/Convênio/Pix) e DEVE SER VALIDADA contra respostas reais do
        /// sandbox/produção antes de confiar neste mapeamento. Qualquer valor não reconhecido
        /// cai em NaoIntegrado, para nunca reportar falsamente Efetivado/Cancelado.
        /// </summary>
        private static BancoBrSituacaoEnum MapSituacaoParaSituacao(string situacao)
        {
            switch (situacao?.Trim().ToUpperInvariant())
            {
                case "EFETIVADO":
                case "LIQUIDADO":
                case "REALIZADO":
                case "PAGO":
                    return BancoBrSituacaoEnum.Efetivado;

                case "AGENDADO":
                case "EM_PROCESSAMENTO":
                case "PROCESSANDO":
                case "PENDENTE":
                    return BancoBrSituacaoEnum.Agendado;

                case "CANCELADO":
                case "DEVOLVIDO":
                    return BancoBrSituacaoEnum.Cancelado;

                case "REJEITADO":
                case "NAO_REALIZADO":
                    return BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        #endregion

        #region ::. Plumbing HTTP .::

        /// <summary>
        /// GET/DELETE desta API envelopam a resposta em { "resultado": [...] } (lista, mesmo
        /// quando há só um item) — diferente do POST, que devolve o objeto direto.
        /// </summary>
        private async Task<T> SendEnvelopedListAsync<T>(HttpMethod method, string url, string idempotencyKey, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body: null, idempotencyKey), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<List<T>>>(body, SerializerSettings);

                return envelope?.Resultado != null && envelope.Resultado.Count > 0 ? envelope.Resultado[0] : default;
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body, string idempotencyKey)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("client_id", _clientId);

            if (idempotencyKey != null)
            {
                request.Headers.Add("x-idempotency-key", idempotencyKey);
            }

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

                // Diferente de Boletos/Convênios/Pix (só Authorization: Bearer), a doc da API
                // SPB Transferências exige também um header "id_token" separado, com o mesmo
                // JWT — confirmado em teste real: sem ele o gateway do Sicoob rejeita com 400
                // "One or more required API parameters are missing" antes de chegar na regra
                // de negócio da TED.
                request.Headers.Add("id_token", token);

                // DEBUG: coloque um breakpoint nesta linha (a de baixo, "return await...") e
                // avalie DebugCurl(request) no Watch/Immediate Window para obter o cURL
                // completo (método, URL, headers e corpo) desta chamada.
                return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// DEBUG ONLY: monta o cURL equivalente à requisição, para reproduzir/compartilhar com
        /// o suporte técnico do banco. Lê request.Content de forma síncrona (.Result) — só
        /// deve ser chamado a partir do Watch/Immediate Window, parado num breakpoint.
        /// </summary>
        private static string DebugCurl(HttpRequestMessage request)
        {
            var curl = new StringBuilder();
            curl.Append("curl -X ").Append(request.Method).Append(" \"").Append(request.RequestUri).Append("\"");

            foreach (var header in request.Headers)
            {
                curl.Append(" -H \"").Append(header.Key).Append(": ").Append(string.Join(",", header.Value)).Append("\"");
            }

            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    curl.Append(" -H \"").Append(header.Key).Append(": ").Append(string.Join(",", header.Value)).Append("\"");
                }

                var body = request.Content.ReadAsStringAsync().Result;
                curl.Append(" -d '").Append(body).Append("'");
            }

            return curl.ToString();
        }

        /// <summary>
        /// Diferente de Boletos/Convênios ({ "mensagens": [{ "codigo", "mensagem" }] }), a API
        /// SPB Transferências devolve erro como um array bruto de objetos
        /// { "code", "title", "detail" } — sem envelope.
        /// </summary>
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var mensagens = new List<MensagemErro>();

            try
            {
                var erros = JsonConvert.DeserializeObject<List<TedErrorItem>>(body, SerializerSettings);
                if (erros != null)
                {
                    foreach (var erro in erros)
                    {
                        mensagens.Add(new MensagemErro
                        {
                            Codigo = erro.Code,
                            Mensagem = !string.IsNullOrWhiteSpace(erro.Detail) ? erro.Detail : erro.Title,
                        });
                    }
                }
            }
            catch (JsonException)
            {
                // Corpo não é o array [{code,title,detail}] esperado — cai no fallback abaixo.
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

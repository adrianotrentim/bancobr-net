using System;
using System.Collections.Generic;
using System.Linq;
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
using BancoBr.API.Sicoob.Pagamentos.Convenios.Models;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios
{
    /// <summary>
    /// Cliente para a API "Convênios Pagamentos" do Sicoob, v2 — bloco de Arrecadação por
    /// código de barras (pagamento de convênios/tributos via código de barras).
    ///
    /// Esta classe é o limite de mapeamento entre o contrato público
    /// (<see cref="Movimento"/>/<see cref="MovimentoItemPagamentoConvenioCodigoBarra"/>,
    /// compartilhado com o CNAB) e os DTOs de wire do Sicoob (<c>Convenios.Models.*</c>,
    /// detalhe de implementação) — mesmo papel que BancoBr.CNAB.Base.Banco tem para os
    /// Segmentos, e igualmente por composição, nunca por herança.
    /// </summary>
    internal class PagamentoConvenioClient : PagamentoConvenioApiBase
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Convênios Pagamentos v2) — não fazem sentido como configuração vinda do
        /// consumidor (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/convenios-pagamentos/v2");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "convenios_consulta", "convenios_escrita" };

        private const int RequestsPerSecond = 2;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoConvenioClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal PagamentoConvenioClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public PagamentoConvenioClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
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

        /// <summary>
        /// <paramref name="origem"/> e <paramref name="unidade"/> não são enviados: o endpoint
        /// de consulta do Sicoob (GET /arrecadacao/codigo-barras/{codigoBarras}) só aceita
        /// dataPagamento e recebimentoViaCaixa. Ficam na assinatura por simetria com as demais
        /// operações da família (e porque outros bancos podem exigi-los).
        /// </summary>
        internal override async Task<Movimento> ConsultarCodigoBarrasAsync(Movimento movimento, Correntista origem, bool? recebimentoViaCaixa = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            var url = $"{_baseUrl}arrecadacao/codigo-barras/{item.CodigoBarra}?dataPagamento={movimento.DataPagamento:yyyy-MM-dd}";
            if (recebimentoViaCaixa.HasValue)
            {
                url += $"&recebimentoViaCaixa={recebimentoViaCaixa.Value.ToString().ToLowerInvariant()}";
            }

            var dto = await SendAsync<ConvenioConsultaResponse>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);

            if (dto == null)
                return movimento;

            item.Convenio = dto.Convenio;
            item.SiglaConvenio = dto.SiglaConvenio;
            item.ValorDocumento = dto.ValorDocumento;
            item.ValorDesconto = dto.ValorDesconto;
            item.ValorMulta = dto.ValorMulta;
            item.ValorJuros = dto.ValorJuros;
            item.ValorOutrosEncargos = dto.ValorOutrosEncargos;
            item.CodigoConvenioFebraban = dto.CodigoConvenioFebraban;
            item.Nsu = dto.Nsu;
            item.Transacao = dto.Transacao;

            if (recebimentoViaCaixa.HasValue)
                item.RecebimentoViaCaixa = recebimentoViaCaixa;

            movimento.ValorPagamento = dto.ValorTotal;

            if (dto.Nsu.HasValue)
                movimento.NumeroDocumentoNoBanco = dto.Nsu.Value.ToString();

            return movimento;
        }

        internal override async Task<Movimento> PagarConvenioAsync(Movimento movimento, Correntista origem, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var url = $"{_baseUrl}arrecadacao/codigo-barras/{item.CodigoBarra}/pagamentos";
            var wireRequest = MontarRequisicaoPagamento(movimento, item, origem, unidade);
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Post, url, json), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Agendado;
                    movimento.DetalheRejeicaoBancoBr = "Pagamento pendente de assinatura.";
                    return movimento;
                }

                try
                {
                    await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
                }
                catch (BancoApiException ex) when (ex.Mensagens.Any(m => m.Codigo == SicoobErrorCodes.IdempotencyJaUtilizado))
                {
                    return await RecuperarPagamentoJaEfetivadoAsync(movimento, item, wireRequest, cancellationToken).ConfigureAwait(false);
                }

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<ArrecadacaoResultado>>(body, SerializerSettings);

                if (envelope?.Resultado != null)
                {
                    item.ComprovanteBase64 = envelope.Resultado.Comprovante;
                    AplicarArrecadacao(movimento, item, envelope.Resultado.Arrecadacao);
                }

                movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Efetivado;
                return movimento;
            }
        }

        /// <summary>
        /// Recupera o comprovante de um pagamento de convênio já efetivado em tentativa anterior
        /// (erro de negócio "idempotency/transação já utilizada"), consultando os pagamentos já
        /// registrados para o código de barras na mesma data de movimento e filtrando pela mesma
        /// transação enviada no pedido original.
        /// </summary>
        private async Task<Movimento> RecuperarPagamentoJaEfetivadoAsync(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item, ArrecadacaoPagamentoRequest wireRequest, CancellationToken cancellationToken)
        {
            var pagamentos = await ConsultarPagamentosAsync(
                item.CodigoBarra,
                wireRequest.Identificacao.Instituicao,
                wireRequest.Pagamento.DataPagamento,
                wireRequest.Transacao,
                cancellationToken).ConfigureAwait(false);

            var encontrado = pagamentos?.FirstOrDefault(p => ((MovimentoItemPagamentoConvenioCodigoBarra)p.MovimentoItem).Transacao == wireRequest.Transacao);
            if (encontrado == null)
            {
                throw new BancoApiException((int)System.Net.HttpStatusCode.Conflict, new List<MensagemErro>
                {
                    new MensagemErro
                    {
                        Codigo = SicoobErrorCodes.IdempotencyJaUtilizado,
                        Mensagem = "Pagamento de convênio já efetivado, mas não foi possível localizar o comprovante para recuperação.",
                    },
                });
            }

            var encontradoItem = (MovimentoItemPagamentoConvenioCodigoBarra)encontrado.MovimentoItem;

            // A consulta de pagamentos não devolve o PDF do comprovante — só os dados da
            // arrecadação; o mesmo já valia antes desta migração (Comprovante = null).
            movimento.ValorPagamento = encontrado.ValorPagamento;
            movimento.DataPagamento = encontrado.DataPagamento;

            if (encontradoItem.Nsu.HasValue)
                movimento.NumeroDocumentoNoBanco = encontradoItem.Nsu.Value.ToString();

            item.Nsu = encontradoItem.Nsu;
            item.ValorDocumento = encontradoItem.ValorDocumento;
            item.ValorDesconto = encontradoItem.ValorDesconto;
            item.ValorJuros = encontradoItem.ValorJuros;
            item.ValorMulta = encontradoItem.ValorMulta;
            item.Autenticacao = encontradoItem.Autenticacao;
            item.RecebimentoViaCaixa = encontradoItem.RecebimentoViaCaixa;
            item.IdentificadorFgts = encontradoItem.IdentificadorFgts;
            item.AnoExercicio = encontradoItem.AnoExercicio;
            item.Convenio = encontradoItem.Convenio;
            item.SiglaConvenio = encontradoItem.SiglaConvenio;
            item.Transacao = encontradoItem.Transacao;

            movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Efetivado;
            return movimento;
        }

        internal override async Task<IReadOnlyList<Movimento>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/codigo-barras/{codigoBarras}/pagamentos?instituicao={instituicao}&dataMovimento={dataMovimento:yyyy-MM-dd}";
            if (transacao.HasValue)
            {
                url += $"&transacao={transacao.Value}";
            }

            var itens = await SendAsync<List<ArrecadacaoConsultaItem>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return itens?.Select(dto => MapArrecadacaoParaMovimento(codigoBarras, dto)).ToList();
        }

        internal override async Task<Movimento> ConsultarComprovantePorNsuAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var nsu = ObterNsu(movimento, item);

            var url = $"{_baseUrl}arrecadacao/pagamentos/{nsu}/comprovante?instituicao={origem.NumeroAgencia}";
            var dto = await SendAsync<ComprovanteArrecadacao>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);

            if (dto == null)
                return movimento;

            item.ComprovanteBase64 = dto.Comprovante;
            AplicarArrecadacao(movimento, item, dto.Pagamento);

            return movimento;
        }

        internal override async Task<IReadOnlyList<Base.Models.ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/conciliacoes?dataMovimento={dataMovimento:yyyy-MM-dd}&instituicao={instituicao}";
            if (unidade.HasValue)
            {
                url += $"&unidade={unidade.Value}";
            }

            var itens = await SendAsync<List<ConciliacaoItem>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return itens?.Select(dto => new Base.Models.ConciliacaoItem
            {
                Situacao = dto.Situacao,
                Convenio = dto.Convenio,
                SiglaConvenio = dto.SiglaConvenio,
                ValorTotal = dto.ValorTotal,
                Quantidade = dto.Quantidade,
            }).ToList();
        }

        internal override async Task<IReadOnlyList<Base.Models.ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}arrecadacao/convenios-habilitados?transacao={transacao}&instituicao={instituicao}";
            var itens = await SendAsync<List<ConvenioHabilitado>>(HttpMethod.Get, url, body: null, cancellationToken)
                .ConfigureAwait(false);
            return itens?.Select(dto => new Base.Models.ConvenioHabilitado
            {
                Identificador = dto.Identificador,
                Sigla = dto.Sigla,
                CodigoFebraban = dto.CodigoFebraban,
                Segmento = dto.Segmento,
            }).ToList();
        }

        #endregion

        #region ::. Mapeamento Movimento <-> Sicoob .::

        private static MovimentoItemPagamentoConvenioCodigoBarra ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra;
            if (item == null)
                throw new InvalidOperationException("As operações de convênio esperam um MovimentoItemPagamentoConvenioCodigoBarra em Movimento.MovimentoItem.");

            return item;
        }

        private static long ObterNsu(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item)
        {
            if (item.Nsu.HasValue)
                return item.Nsu.Value;

            long nsu;
            if (!string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco) && long.TryParse(movimento.NumeroDocumentoNoBanco, out nsu))
                return nsu;

            throw new InvalidOperationException("Para consultar o comprovante, o movimento precisa ter o NSU da arrecadação (MovimentoItem.Nsu ou NumeroDocumentoNoBanco).");
        }

        private static ArrecadacaoPagamentoRequest MontarRequisicaoPagamento(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item, Correntista origem, int unidade) => new ArrecadacaoPagamentoRequest
        {
            Identificacao = new Identificacao
            {
                // A "instituição" da arrecadação é a cooperativa/agência da conta pagadora,
                // mesmo número usado como DebtorAccount.Issuer no pagamento de boletos.
                Instituicao = origem.NumeroAgencia,
                Unidade = unidade,
            },
            Pagamento = new PagamentoConvenio
            {
                ValorPago = movimento.ValorPagamento,
                Nsu = item.Nsu,
                DataPagamento = movimento.DataPagamento,
                ValorDocumento = item.ValorDocumento,
                ValorDesconto = item.ValorDesconto,
                ValorJuros = item.ValorJuros,
                ValorMulta = item.ValorMulta,
                RecebimentoViaCaixa = item.RecebimentoViaCaixa,
            },
            // Devolvida pela consulta do código de barras; é o que o Sicoob usa para deduplicar
            // reenvios (e o que permite recuperar um pagamento já efetivado).
            Transacao = item.Transacao.GetValueOrDefault(),
        };

        private static void AplicarArrecadacao(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item, Arrecadacao arrecadacao)
        {
            if (arrecadacao == null)
                return;

            if (arrecadacao.ValorPago != 0)
                movimento.ValorPagamento = arrecadacao.ValorPago;

            if (arrecadacao.DataPagamento != default(DateTime))
                movimento.DataPagamento = arrecadacao.DataPagamento;

            if (arrecadacao.Nsu.HasValue)
                movimento.NumeroDocumentoNoBanco = arrecadacao.Nsu.Value.ToString();

            item.Nsu = arrecadacao.Nsu ?? item.Nsu;
            item.ValorDocumento = arrecadacao.ValorDocumento;
            item.ValorDesconto = arrecadacao.ValorDesconto;
            item.ValorJuros = arrecadacao.ValorJuros;
            item.ValorMulta = arrecadacao.ValorMulta;
            item.Autenticacao = arrecadacao.Autenticacao;
            item.RecebimentoViaCaixa = arrecadacao.RecebimentoViaCaixa ?? item.RecebimentoViaCaixa;
        }

        /// <summary>
        /// ATENÇÃO: mapeamento best-effort do campo "situacao.descricao" (e, em caráter secundário,
        /// "situacao.codigo") retornado pela API de Arrecadação/Convênios do Sicoob para o enum
        /// agnóstico <see cref="BancoBrSituacaoEnum"/>. O único valor
        /// confirmado em testes/documentação disponível neste repositório é descricao "Recebido"
        /// com codigo 0. Os demais valores abaixo foram inferidos a partir do vocabulário plausível
        /// de status de arrecadação e NÃO estão confirmados — DEVEM SER VALIDADOS/AJUSTADOS contra
        /// respostas reais do sandbox/produção do Sicoob antes de confiar neste mapeamento. Qualquer
        /// valor não reconhecido cai em NaoIntegrado, para nunca reportar falsamente
        /// Efetivado/Cancelado.
        /// </summary>
        private static BancoBrSituacaoEnum MapSituacaoArrecadacaoParaSituacao(SituacaoArrecadacao situacao)
        {
            if (situacao == null) return BancoBrSituacaoEnum.NaoIntegrado;

            switch (situacao.Descricao?.Trim().ToUpperInvariant())
            {
                case "RECEBIDO":
                case "PAGO":
                case "EFETIVADO":
                    return BancoBrSituacaoEnum.Efetivado;

                case "AGENDADO":
                case "PENDENTE":
                case "EM_PROCESSAMENTO":
                    return BancoBrSituacaoEnum.Agendado;

                case "CANCELADO":
                case "ESTORNADO":
                    return BancoBrSituacaoEnum.Cancelado;

                case "REJEITADO":
                    return BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        private static Movimento MapArrecadacaoParaMovimento(string codigoBarras, ArrecadacaoConsultaItem dto) => new Movimento
        {
            ValorPagamento = dto.ValorPago,
            DataPagamento = dto.DataPagamento,
            NumeroDocumentoNoBanco = dto.Nsu?.ToString(),
            SituacaoBancoBr = MapSituacaoArrecadacaoParaSituacao(dto.Situacao),
            DetalheRejeicaoBancoBr = dto.Situacao?.Descricao,
            MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra
            {
                CodigoBarra = codigoBarras,
                ValorDocumento = dto.ValorDocumento,
                ValorDesconto = dto.ValorDesconto,
                ValorJuros = dto.ValorJuros,
                ValorMulta = dto.ValorMulta,
                Nsu = dto.Nsu,
                Transacao = dto.Transacao,
                RecebimentoViaCaixa = dto.RecebimentoViaCaixa,
                Autenticacao = dto.Autenticacao,
                IdentificadorFgts = dto.IdentificadorFgts,
                AnoExercicio = dto.AnoExercicio,
                Convenio = dto.Convenio,
                SiglaConvenio = dto.SiglaConvenio,
            },
        };

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

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<T>>(responseBody, SerializerSettings);
                return envelope.Resultado;
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("client_id", _clientId);

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

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            SicoobErrorResponse errorResponse;
            try
            {
                errorResponse = JsonConvert.DeserializeObject<SicoobErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                errorResponse = null;
            }

            throw new BancoApiException((int)response.StatusCode, errorResponse?.Mensagens ?? new List<MensagemErro>());
        }

        #endregion
    }
}

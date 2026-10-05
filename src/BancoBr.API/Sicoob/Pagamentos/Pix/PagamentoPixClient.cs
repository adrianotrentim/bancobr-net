using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Pix.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix
{
    /// <summary>
    /// Cliente para a API "Pix Pagamentos" do Sicoob, v2 — iniciação de pagamento por
    /// chave DICT e confirmação. Webhook não é implementado.
    ///
    /// Esta classe é o limite de mapeamento entre o contrato público
    /// (<see cref="Movimento"/>/<see cref="MovimentoItem"/>, compartilhado com o CNAB) e os
    /// DTOs de wire do Sicoob (<c>Pix.Models.*</c>, detalhe de implementação) — mesmo papel
    /// que BancoBr.CNAB.Base.Banco tem para os Segmentos, e igualmente por composição, nunca
    /// por herança.
    /// </summary>
    internal class PagamentoPixClient : PagamentoPixApiBase
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Pix Pagamentos v2) — não fazem sentido como configuração vinda do consumidor
        /// (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pix-pagamentos/v2");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "pixpagamentos_escrita", "pixpagamentos_consulta" };

        /// <summary>
        /// ISPB do Sicoob (Banco Cooperativo do Brasil), usado como "origem.ispb" no
        /// pagamento via QR Code. O Sicoob exige exatamente 8 caracteres
        /// (padrão ^[0-9A-Z]{8}$), por isso com zero à esquerda.
        /// </summary>
        private const string SicoobIspb = "54037916";

        /// <summary>
        /// Valor de "origem.tipo" no pagamento via QR Code. O <see cref="Correntista"/> não
        /// carrega tipo de conta, e a única modalidade habilitada para pagamento via API é a
        /// conta corrente — mesmo valor que o consumidor já enviava antes desta migração.
        /// </summary>
        private const string TipoContaCorrenteQrCode = "CORRENTE";

        /// <summary>
        /// Rate limit documentado para a API Pix Pagamentos: 1 requisição por segundo
        /// (diferente das APIs de Boletos/Convênios, que permitem 2/s).
        /// </summary>
        private const int RequestsPerSecond = 1;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoPixClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal PagamentoPixClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
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

        internal override async Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar um pagamento Pix, o movimento precisa ter o EndToEndId em NumeroDocumentoNoBanco.");

            var url = $"{_baseUrl}pagamentos/{movimento.NumeroDocumentoNoBanco}";
            var dto = await SendAsync<RetornoPagamento>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);

            return AplicarRetornoPagamento(movimento, dto);
        }

        internal override async Task<Movimento> IniciarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
            if (item == null)
                throw new InvalidOperationException("IniciarPagamentoAsync espera um MovimentoItemTransferenciaPIX em Movimento.MovimentoItem.");

            if (string.IsNullOrWhiteSpace(item.ChavePIX))
                throw new InvalidOperationException("A chave Pix não foi informada no movimento.");

            var url = $"{_baseUrl}pagamentos";
            var wireRequest = new RequisicaoIniciacaoPix
            {
                Chave = item.ChavePIX,
                DataAgendamento = NormalizarDataAgendamento(movimento.DataPagamento),
            };
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            var dto = await SendAsync<PixIniciacaoResponse>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);
            if (dto == null)
                return movimento;

            movimento.NumeroDocumentoNoBanco = dto.EndToEndId;

            if (!string.IsNullOrWhiteSpace(dto.Chave))
                item.ChavePIX = dto.Chave;

            item.TipoChaveConfirmado = dto.Tipo;

            if (dto.Proprietario != null)
            {
                item.TitularConfirmadoNome = dto.Proprietario.Nome;
                item.TitularConfirmadoDocumento = dto.Proprietario.Identificador;
                item.TitularConfirmadoTipo = dto.Proprietario.Tipo;
            }

            return movimento;
        }

        internal override async Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para confirmar um pagamento Pix, o movimento precisa ter o EndToEndId (devolvido por IniciarPagamentoAsync) em NumeroDocumentoNoBanco.");

            var url = $"{_baseUrl}pagamentos/confirmacao";
            var wireRequest = new RequisicaoEfetivacaoPix
            {
                EndToEndId = movimento.NumeroDocumentoNoBanco,
                Valor = movimento.ValorPagamento.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','),
                Descricao = movimento.NumeroDocumento,
                DataAgendamento = NormalizarDataAgendamento(movimento.DataPagamento),
            };
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            var dto = await SendAsync<RetornoPagamento>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);

            return AplicarRetornoPagamento(movimento, dto);
        }

        internal override async Task<Movimento> PagarViaQrCodeAsync(Correntista origem, Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoTituloPIXQRCode;
            if (item == null)
                throw new InvalidOperationException("PagarViaQrCodeAsync espera um MovimentoItemPagamentoTituloPIXQRCode em Movimento.MovimentoItem.");

            var url = $"{_baseUrl}pagamentos/qrcode";
            var wireRequest = new RequisicaoPagamentoQrCode
            {
                QrCode = item.QrCode,
                Repeticao = item.Repeticao,
                DataVencimento = item.DataVencimento == default(DateTime) ? (DateTime?)null : item.DataVencimento,
                DataAgendamento = NormalizarDataAgendamento(movimento.DataPagamento),
                Valor = movimento.ValorPagamento.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                PagarComJurosMulta = item.PagarComJurosMulta,
                Origem = new DadosOrigemQrCode
                {
                    Ispb = SicoobIspb,
                    CpfCnpj = origem.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                    Nome = origem.Nome,
                    Conta = $"{origem.NumeroConta}{origem.DVConta}",
                    Agencia = origem.NumeroAgencia.ToString(),
                    Tipo = TipoContaCorrenteQrCode,
                },
                Destino = movimento.Favorecido == null ? null : new DadosDestinatarioQrCode
                {
                    CpfCnpj = movimento.Favorecido.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                },
            };
            var json = JsonConvert.SerializeObject(wireRequest, SerializerSettings);

            var dto = await SendAsync<PagamentoQrCodeResponse>(HttpMethod.Post, url, json, cancellationToken).ConfigureAwait(false);
            if (dto == null)
                return movimento;

            AplicarCamposComuns(movimento, dto.EndToEndId, dto.Estado, dto.Valor, dto.DetalheRejeicao, dto.Descricao, dto.Horario, dto.DataAgendamento);

            item.TXID = dto.TxId;
            item.ValorOriginal = dto.ValorOriginal;
            item.Abatimento = dto.Abatimento;
            item.Desconto = dto.Desconto;
            item.Multa = dto.Multa;
            item.Juros = dto.Juros;
            item.TipoQrcode = dto.TipoQrcode;

            return movimento;
        }

        internal override async Task<Movimento> PagarComIniciacaoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            await IniciarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);

            return await ConfirmarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region ::. Mapeamento de resposta .::

        private static Movimento AplicarRetornoPagamento(Movimento movimento, RetornoPagamento dto)
        {
            if (dto == null)
                return movimento;

            AplicarCamposComuns(movimento, dto.EndToEndId, dto.Estado, dto.Valor, dto.DetalheRejeicao, dto.Descricao, dto.Horario, dto.DataAgendamento);

            // "destino" identifica o titular efetivamente creditado — a mesma entidade que a
            // iniciação por chave devolve em "proprietario". Nome e documento são mapeados; o
            // "tipo" NÃO é, porque aqui significa tipo de conta, e não tipo de pessoa como em
            // PixProprietario.Tipo — sobrescrevê-lo corromperia TitularConfirmadoTipo.
            var itemPix = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
            if (itemPix != null && dto.Destino != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.Destino.Nome))
                    itemPix.TitularConfirmadoNome = dto.Destino.Nome;

                if (!string.IsNullOrWhiteSpace(dto.Destino.CpfCnpj))
                    itemPix.TitularConfirmadoDocumento = dto.Destino.CpfCnpj;

                if (!string.IsNullOrWhiteSpace(dto.Destino.ChaveDict))
                    itemPix.ChavePIX = dto.Destino.ChaveDict;
            }

            return movimento;
        }

        private static void AplicarCamposComuns(Movimento movimento, string endToEndId, string estado, decimal valor, string detalheRejeicao, string descricao, DateTime? horario, DateTime? dataAgendamento)
        {
            if (!string.IsNullOrWhiteSpace(endToEndId))
                movimento.NumeroDocumentoNoBanco = endToEndId;

            movimento.SituacaoBancoBr = MapEstadoParaSituacao(estado);
            movimento.DetalheRejeicaoBancoBr = detalheRejeicao;
            movimento.HorarioBancoBr = horario;

            if (valor != 0)
                movimento.ValorPagamento = valor;

            if (!string.IsNullOrWhiteSpace(descricao))
                movimento.NumeroDocumento = descricao;

            if (dataAgendamento.HasValue)
                movimento.DataPagamento = dataAgendamento.Value;
        }

        /// <summary>
        /// O Sicoob rejeita "dataAgendamento" quando a data não é estritamente posterior a
        /// hoje ("A data de agendamento deve ser maior do que o dia corrente."). Para
        /// pagamento no mesmo dia, o campo deve ser omitido do payload (liquidação imediata),
        /// em vez de enviado com a data de hoje. Um <see cref="Movimento"/> sem data de
        /// pagamento (default) também vira liquidação imediata.
        /// </summary>
        private static DateTime? NormalizarDataAgendamento(DateTime dataAgendamento) =>
            dataAgendamento == default(DateTime) || dataAgendamento.Date <= DateTime.Today
                ? (DateTime?)null
                : dataAgendamento;

        /// <summary>
        /// ATENÇÃO: mapeamento best-effort do campo textual "Estado" retornado pela API Pix
        /// Pagamentos do Sicoob para o enum agnóstico <see cref="BancoBrSituacaoEnum"/>.
        /// Os valores exatos possíveis de "Estado" NÃO estão confirmados neste código nem em
        /// documentação disponível no momento da implementação — esta lista foi construída a
        /// partir do vocabulário plausível de status de pagamento Pix. DEVE SER
        /// VALIDADO/AJUSTADO contra respostas reais do sandbox/produção do Sicoob antes de
        /// confiar neste mapeamento. Qualquer valor não reconhecido cai em NaoIntegrado, para
        /// nunca reportar falsamente Efetivado/Cancelado.
        /// </summary>
        private static BancoBrSituacaoEnum MapEstadoParaSituacao(string estado)
        {
            switch (estado?.Trim().ToUpperInvariant())
            {
                case "REALIZADO":
                case "LIQUIDADO":
                case "EFETIVADO":
                case "FINALIZADO":
                    return BancoBrSituacaoEnum.Efetivado;

                case "AGENDADO":
                case "EM_PROCESSAMENTO":
                case "PROCESSANDO":
                    return BancoBrSituacaoEnum.Agendado;

                case "NAO_REALIZADO":
                case "CANCELADO":
                case "DEVOLVIDO":
                    return BancoBrSituacaoEnum.Cancelado;

                case "REJEITADO":
                    return BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        #endregion

        #region ::. Plumbing HTTP .::

        /// <summary>
        /// Ao contrário das APIs de Boletos/Convênios, a Pix Pagamentos não envelopa as
        /// respostas em { "resultado": ... } — o corpo já é o objeto desejado.
        /// </summary>
        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<T>(responseBody, SerializerSettings);
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

        /// <summary>
        /// A Pix Pagamentos deveria retornar erros em application/problem+json
        /// ({ type, title, status, detail, violacoes[] }), diferente do
        /// { mensagens: [{ codigo, mensagem }] } usado por Boletos/Convênios — mas na
        /// prática o Sicoob também já respondeu nesse segundo formato para este endpoint.
        /// Como o Json.NET desserializa objetos JSON silenciosamente ignorando propriedades
        /// que não existem no tipo alvo (sem lançar exceção), tentar só o formato
        /// problem+json quando o corpo é, na verdade, { mensagens: [...] } resulta em um
        /// <see cref="PixErrorResponse"/> com todos os campos nulos — e a mensagem de erro
        /// real do banco era descartada silenciosamente. Por isso tentamos os dois formatos
        /// e, se nenhum produzir informação útil, caímos para o corpo bruto da resposta,
        /// para nunca esconder a causa real de uma falha do Sicoob.
        /// </summary>
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var mensagens = new System.Collections.Generic.List<MensagemErro>();

            PixErrorResponse problemResponse;
            try
            {
                problemResponse = JsonConvert.DeserializeObject<PixErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                problemResponse = null;
            }

            if (problemResponse != null && (!string.IsNullOrEmpty(problemResponse.Title) || !string.IsNullOrEmpty(problemResponse.Detail)))
            {
                mensagens.Add(new MensagemErro { Codigo = problemResponse.Title, Mensagem = problemResponse.Detail });
            }

            if (problemResponse?.Violacoes != null)
            {
                foreach (var violacao in problemResponse.Violacoes)
                {
                    mensagens.Add(new MensagemErro { Codigo = violacao.Propriedade, Mensagem = violacao.Razao });
                }
            }

            if (mensagens.Count == 0)
            {
                SicoobErrorResponse envelopeResponse;
                try
                {
                    envelopeResponse = JsonConvert.DeserializeObject<SicoobErrorResponse>(body, SerializerSettings);
                }
                catch (JsonException)
                {
                    envelopeResponse = null;
                }

                if (envelopeResponse?.Mensagens != null)
                {
                    mensagens.AddRange(envelopeResponse.Mensagens);
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

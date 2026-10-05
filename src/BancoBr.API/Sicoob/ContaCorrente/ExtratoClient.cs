using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.ContaCorrente.Models;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.ContaCorrente
{
    /// <summary>
    /// Cliente para a API "Conta Corrente" do Sicoob, v4 (saldo/extrato).
    /// </summary>
    internal class ExtratoClient : ContaCorrenteApiBase
    {
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/conta-corrente/v4");

        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "cco_consulta" };

        private const int RequestsPerSecond = 2;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal ExtratoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        internal ExtratoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public ExtratoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
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

        internal override async Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}extrato/{mes}/{ano}?numeroContaCorrente={numeroContaCorrente}&agruparCNAB={(agruparCnab ? "true" : "false")}";

            if (diaInicial.HasValue)
                url += $"&diaInicial={diaInicial.Value}";

            if (diaFinal.HasValue)
                url += $"&diaFinal={diaFinal.Value}";

            using (var response = await SendWithAuthAsync(() => BuildRequest(url), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return Array.Empty<Movimento>();

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<ExtratoResponse>>(body, SerializerSettings);

                return MapExtrato(envelope?.Resultado);
            }
        }

        #endregion

        #region ::. Mapeamento wire -> agnóstico .::

        private static IReadOnlyList<Movimento> MapExtrato(ExtratoResponse wire)
        {
            if (wire == null)
                return Array.Empty<Movimento>();

            var resultado = new List<Movimento>
            {
                new Movimento
                {
                    MovimentoItem = new MovimentoItemExtratoSaldo
                    {
                        SaldoAtual = ParseDecimal(wire.SaldoAtual),
                        SaldoBloqueado = ParseDecimal(wire.SaldoBloqueado),
                        SaldoLimite = ParseDecimal(wire.SaldoLimite),
                        SaldoAnterior = ParseDecimal(wire.SaldoAnterior),
                        SaldoBloqueioJudicial = ParseDecimal(wire.SaldoBloqueioJudicial),
                        SaldoBloqueioJudicialAnterior = ParseDecimal(wire.SaldoBloqueioJudicialAnterior),
                    },
                },
            };

            if (wire.Transacoes != null)
                resultado.AddRange(wire.Transacoes.Select(MapTransacao));

            return resultado;
        }

        private static Movimento MapTransacao(TransacaoResponse dto) => new Movimento
        {
            MovimentoItem = new MovimentoItemExtratoTransacao
            {
                TransactionId = dto.TransactionId,
                Tipo = MapTipo(dto.Tipo),
                Valor = ParseDecimal(dto.Valor),
                Data = ParseData(dto.Data),
                DataLote = string.IsNullOrWhiteSpace(dto.DataLote) ? (DateTime?)null : ParseData(dto.DataLote),
                Descricao = dto.Descricao,
                NumeroDocumento = dto.NumeroDocumento,
                CpfCnpj = dto.CpfCnpj,
                DescricaoInformacaoComplementar = dto.DescInfComplementar,
            },
        };

        /// <summary>
        /// Confirmado contra resposta real do Sicoob: o campo "tipo" do extrato vem como
        /// "CREDITO" ou "DEBITO". Sem fallback aqui — diferente das situações de pagamento
        /// (que podem cair em "NaoIntegrado"), errar o sinal de um lançamento financeiro é
        /// pior do que falhar a importação.
        /// </summary>
        private static BancoBrTipoCreditoDebitoEnum MapTipo(string tipo)
        {
            switch (tipo?.Trim().ToUpperInvariant())
            {
                case "CREDITO":
                    return BancoBrTipoCreditoDebitoEnum.Credito;

                case "DEBITO":
                    return BancoBrTipoCreditoDebitoEnum.Debito;

                default:
                    throw new Exception($"Tipo de lançamento não reconhecido no extrato Sicoob: \"{tipo}\".");
            }
        }

        private static decimal ParseDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return 0;

            return decimal.Parse(valor, NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Confirmado contra resposta real do Sicoob: "data" vem como "yyyy-MM-ddTHH:mm" e
        /// "dataLote" como "yyyy-MM-dd" — ambos ISO 8601, sem timezone. <see cref="DateTime.Parse(string, IFormatProvider, DateTimeStyles)"/>
        /// já cobre as duas variantes.
        /// </summary>
        private static DateTime ParseData(string data)
        {
            return DateTime.Parse(data, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        #endregion

        #region ::. Plumbing HTTP .::

        private HttpRequestMessage BuildRequest(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("client_id", _clientId);
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
        /// Diferente de Boletos/Convênios ({ "mensagens": [{ "codigo", "mensagem" }] }), a API
        /// Conta Corrente devolve erro como { "errors": [{ "code", "title", "detail" }], "meta": {...} }.
        /// </summary>
        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var mensagens = new List<MensagemErro>();

            try
            {
                var errorResponse = JsonConvert.DeserializeObject<ContaCorrenteErrorResponse>(body, SerializerSettings);
                if (errorResponse?.Errors != null)
                {
                    foreach (var erro in errorResponse.Errors)
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
                // Corpo não é o { "errors": [...] } esperado — cai no fallback abaixo.
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

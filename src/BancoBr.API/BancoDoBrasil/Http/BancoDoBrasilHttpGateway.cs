using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.BancoDoBrasil.Http
{
    /// <summary>Transporte autenticado das APIs BB, com App Key e renovação no HTTP 401.</summary>
    internal class BancoDoBrasilHttpGateway
    {
        public static readonly Uri PagamentosBaseUrl = new Uri("https://pagamentos-lote.mtls.api.bb.com.br/v1/");
        public static readonly Uri DefaultTokenEndpoint = new Uri("https://oauth.bb.com.br/oauth/token");
        private readonly HttpClient _http;
        private readonly IAccessTokenProvider _tokens;
        private readonly string _chave;

        public BancoDoBrasilHttpGateway(HttpClient httpClient, IAccessTokenProvider tokenProvider, string chaveAplicacao)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokens = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _chave = !string.IsNullOrWhiteSpace(chaveAplicacao) ? chaveAplicacao : throw new ArgumentException("Informe a chave da aplicação BB.", nameof(chaveAplicacao));
        }

        public static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificateSource.GetCertificate());
            return new HttpClient(handler);
        }

        public async Task<T> SendAsync<T>(HttpMethod method, string url, object body, CancellationToken cancellationToken)
        {
            var json = body == null ? null : JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            for (var tentativa = 0; ; tentativa++)
            {
                using (var request = new HttpRequestMessage(method, url + (url.Contains("?") ? "&" : "?") + "gw-dev-app-key=" + Uri.EscapeDataString(_chave)))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false));
                    if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized && tentativa == 0) { _tokens.InvalidateToken(); continue; }
                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        if (!response.IsSuccessStatusCode)
                        {
                            MensagemErro[] erros = null;
                            try { var error = JObject.Parse(content); erros = error["errors"]?.ToObject<MensagemErro[]>(); if (erros == null && error["message"] != null) erros = new[] { new MensagemErro { Codigo = (string)error["code"], Mensagem = (string)error["message"] } }; } catch (JsonException) { }
                            throw new BancoApiException((int)response.StatusCode, erros?.Length > 0 ? erros : new[] { new MensagemErro { Codigo = ((int)response.StatusCode).ToString(), Mensagem = response.ReasonPhrase } });
                        }
                        return string.IsNullOrWhiteSpace(content) ? default : JsonConvert.DeserializeObject<T>(content);
                    }
                }
            }
        }
    }
}

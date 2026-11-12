using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.Http
{
    /// <summary>
    /// Transporte autenticado das APIs do Bradesco: mTLS, Bearer com renovação do token no HTTP 401 e
    /// normalização dos corpos de erro em <see cref="BancoApiException"/>.
    /// </summary>
    internal class BradescoHttpGateway
    {
        /// <summary>Host de Boleto e TED em produção (Portal Developers Bradesco).</summary>
        public static readonly Uri PagamentosBaseUrl = new Uri("https://openapi.bradesco.com.br/");

        /// <summary>Host de Pix em produção.</summary>
        public static readonly Uri PixBaseUrl = new Uri("https://qrpix.bradesco.com.br/");

        /// <summary>Sandbox compartilhado por todas as APIs.</summary>
        public static readonly Uri HomologacaoBaseUrl = new Uri("https://openapisandbox.prebanco.com.br/");

        public static readonly Uri PagamentosTokenEndpoint = new Uri(PagamentosBaseUrl, "auth/server-mtls/v2/token");
        public static readonly Uri PixTokenEndpoint = new Uri(PixBaseUrl, "auth/server/oauth/token");
        public static readonly Uri HomologacaoTokenEndpoint = new Uri(HomologacaoBaseUrl, "auth/server-mtls/v2/token");

        private readonly HttpClient _http;
        private readonly IAccessTokenProvider _tokens;

        public BradescoHttpGateway(HttpClient httpClient, IAccessTokenProvider tokenProvider)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokens = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        }

        /// <summary>O mesmo HttpClient (com o certificado do cliente) atende o endpoint de token e as APIs: o Bradesco exige mTLS em ambos.</summary>
        public static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual };
            handler.ClientCertificates.Add(certificateSource.GetCertificate());
            return new HttpClient(handler);
        }

        public static string NormalizarBase(Uri baseUrl) => baseUrl.ToString().TrimEnd('/') + "/";

        public async Task<JObject> SendAsync(HttpMethod method, string url, object body, CancellationToken cancellationToken)
        {
            var json = body == null ? null : JsonConvert.SerializeObject(body);
            for (var tentativa = 0; ; tentativa++)
            {
                using (var request = new HttpRequestMessage(method, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false));
                    if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized && tentativa == 0)
                        {
                            _tokens.InvalidateToken();
                            continue;
                        }

                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                            throw new BancoApiException((int)response.StatusCode, ExtrairMensagens((int)response.StatusCode, content));

                        var raiz = string.IsNullOrWhiteSpace(content) ? new JObject() : ParseRaiz(content);

                        // Boleto devolve o status HTTP também no corpo; um 4xx/5xx ali é falha mesmo com HTTP 200.
                        if (raiz["status"] != null && raiz["status"].Type == JTokenType.Integer && (int)raiz["status"] >= 400)
                            throw new BancoApiException((int)raiz["status"], ExtrairMensagens((int)raiz["status"], content));

                        return raiz;
                    }
                }
            }
        }

        private static JObject ParseRaiz(string content)
        {
            try { return JObject.Parse(content); }
            catch (JsonException) { throw new InvalidOperationException("Resposta inesperada do Bradesco (não é um objeto JSON): " + content); }
        }

        /// <summary>
        /// Os formatos de erro variam por API (Boleto: <c>causa</c> "CBCA0204 - mensagem"; TED:
        /// <c>codigoDeErro</c>/<c>mensagem</c>; Pix: <c>codigo</c>/<c>mensagem</c> e listas de violações).
        /// Sem corpo reconhecível, devolve o texto bruto para nunca esconder a causa real.
        /// </summary>
        internal static IReadOnlyList<MensagemErro> ExtrairMensagens(int httpStatus, string content)
        {
            var mensagens = new List<MensagemErro>();
            JObject raiz = null;
            try { raiz = string.IsNullOrWhiteSpace(content) ? null : JObject.Parse(content); }
            catch (JsonException) { }

            if (raiz != null)
            {
                var codigo = Primeiro(raiz, "codigoDeErro", "codigoErro", "codigo", "code", "retorno", "type");
                var mensagem = Primeiro(raiz, "mensagem", "message", "detail", "detalhe", "title", "descricao");
                var causa = Primeiro(raiz, "causa");
                if (causa != null)
                {
                    var partes = causa.Split(new[] { " - " }, 2, StringSplitOptions.None);
                    if (partes.Length == 2)
                    {
                        codigo = codigo ?? partes[0].Trim();
                        mensagem = partes[1].Trim();
                    }
                    else
                    {
                        mensagem = causa;
                    }
                }

                if (codigo != null || mensagem != null)
                    mensagens.Add(new MensagemErro { Codigo = codigo, Mensagem = mensagem });

                foreach (var lista in new[] { "violacoes", "errors", "erros" })
                {
                    if (!(raiz[lista] is JArray itens)) continue;
                    foreach (var item in itens.OfType<JObject>())
                        mensagens.Add(new MensagemErro
                        {
                            Codigo = Primeiro(item, "codigo", "code", "razao"),
                            Mensagem = Primeiro(item, "mensagem", "message", "descricao", "detalhe", "propriedade")
                        });
                }
            }

            if (mensagens.Count == 0)
                mensagens.Add(new MensagemErro
                {
                    Codigo = httpStatus.ToString(),
                    Mensagem = string.IsNullOrWhiteSpace(content) ? ((HttpStatusCode)httpStatus).ToString() : content
                });
            return mensagens;
        }

        private static string Primeiro(JObject objeto, params string[] nomes)
        {
            foreach (var nome in nomes)
            {
                var texto = BradescoCampos.Texto(objeto[nome]);
                if (texto != null) return texto;
            }
            return null;
        }
    }
}

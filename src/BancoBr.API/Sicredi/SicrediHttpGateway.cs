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
using BancoBr.Common.Instances;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Sicredi
{
    /// <summary>Transporte Multipag/conta corrente, com mTLS no HttpClient e renovação de token no 401.</summary>
    internal sealed class SicrediHttpGateway
    {
        private readonly HttpClient _http;
        private readonly IAccessTokenProvider _tokens;
        internal readonly Correntista Origem;
        internal readonly string Raiz;
        internal string Pagamentos => Raiz + "multipag" + (Homologacao ? "-pagamento-sandbox" : "") + "/v1/pagamentos/";
        internal bool Homologacao { get; }

        internal SicrediHttpGateway(HttpClient http, IAccessTokenProvider tokens, Correntista origem, bool homologacao)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            Origem = origem ?? throw new ArgumentNullException(nameof(origem));
            Homologacao = homologacao;
            Raiz = "https://mtls-api-parceiro.sicredi.com.br/" + (homologacao ? "sb/" : "");
        }

        internal static HttpClient CriarHttpClient(CertificateSource certificado)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificado.GetCertificate());
            return new HttpClient(handler);
        }

        internal static string Digitos(string valor) => new string((valor ?? "").Where(c => c >= '0' && c <= '9').ToArray());
        internal JObject Conta() => new JObject
        {
            ["conta"] = Origem.NumeroConta.ToString("D5") + Origem.DVConta,
            ["cooperativa"] = Origem.NumeroAgencia.ToString("D4"),
            ["documento"] = Digitos(Origem.CPF_CNPJ)
        };

        internal async Task<byte[]> SendAsync(HttpMethod method, string url, JObject body, CancellationToken ct, bool pdf = false)
        {
            var json = body?.ToString(Formatting.None);
            for (var tentativa = 0; ; tentativa++)
            {
                using (var request = new HttpRequestMessage(method, url))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetAccessTokenAsync(ct).ConfigureAwait(false));
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(pdf ? "application/pdf" : "application/json"));
                    foreach (var campo in Conta().Properties()) request.Headers.Add("x-" + campo.Name, (string)campo.Value);
                    if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    using (var response = await _http.SendAsync(request, ct).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized && tentativa == 0)
                        {
                            _tokens.InvalidateToken();
                            continue;
                        }
                        if (!response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            JObject erro = null;
                            try { erro = JObject.Parse(content); } catch (JsonException) { }
                            throw new BancoApiException((int)response.StatusCode, new[] { new MensagemErro
                            {
                                Codigo = (string)erro?["codigo"] ?? (string)erro?["code"] ?? ((int)response.StatusCode).ToString(),
                                Mensagem = (string)erro?["detail"] ?? (string)erro?["mensagem"] ?? (string)erro?["message"] ?? response.ReasonPhrase
                            }});
                        }
                        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        internal async Task<JObject> JsonAsync(HttpMethod method, string url, JObject body, CancellationToken ct)
        {
            var bytes = await SendAsync(method, url, body, ct).ConfigureAwait(false);
            return bytes.Length == 0 ? new JObject() : JObject.Parse(Encoding.UTF8.GetString(bytes));
        }
    }
}

using System.Net;
using System.Net.Http;

namespace BancoBr.Tests.Sicoob
{
    /// <summary>
    /// Handler de teste que devolve uma resposta diferente a cada chamada, na ordem fornecida —
    /// usado para validar fluxos de múltiplas requisições (ex.: consulta seguida de pagamento).
    /// </summary>
    internal class SequencedFakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string? Body)> _responses;

        public List<HttpRequestMessage> Requests { get; } = new();

        public SequencedFakeHttpMessageHandler(params (HttpStatusCode StatusCode, string? Body)[] responses)
        {
            _responses = new Queue<(HttpStatusCode, string?)>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            var (statusCode, body) = _responses.Dequeue();
            var response = new HttpResponseMessage(statusCode);
            if (body != null)
            {
                response.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            return Task.FromResult(response);
        }
    }
}

using System.Net;
using System.Net.Http;

namespace BancoBr.Tests.Sicoob
{
    /// <summary>
    /// Handler de teste que devolve respostas pré-definidas, evitando qualquer chamada de rede real.
    /// </summary>
    internal class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _responseBody;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string? responseBody = null)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content != null
                ? await request.Content.ReadAsStringAsync(cancellationToken)
                : null;

            var response = new HttpResponseMessage(_statusCode);
            if (_responseBody != null)
            {
                response.Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json");
            }

            return response;
        }
    }
}

using BancoBr.API.Core.OAuth;

namespace BancoBr.Tests.Sicoob
{
    /// <summary>
    /// Sempre devolve um token fixo, sem fazer nenhuma requisição HTTP real.
    /// </summary>
    internal class FakeOAuthTokenProvider : OAuthTokenProvider
    {
        public FakeOAuthTokenProvider()
            : base(new HttpClient(new FakeHttpMessageHandler(System.Net.HttpStatusCode.OK)), new OAuthTokenProviderOptions
            {
                TokenEndpoint = new Uri("https://auth.sicoob.com.br/token"),
                ClientId = "fake-client-id",
            })
        {
        }

        public override Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("fake-access-token");
        }
    }
}

using System.Net;
using System.Text;
using BancoBr.API.Core.OAuth;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.BancoDoBrasil
{
    public class OAuthTokenProviderTests
    {
        [Fact]
        public async Task GetAccessTokenAsync_Basic_EnviaCredenciaisSomenteNoCabecalho()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"access_token\":\"token-ficticio\",\"expires_in\":3600}");
            var provider = new OAuthTokenProvider(new HttpClient(handler), new OAuthTokenProviderOptions
            {
                TokenEndpoint = new Uri("https://oauth.example.test/oauth/token"),
                ClientId = "cliente-ficticio",
                ClientSecret = "segredo-ficticio",
                Scopes = new[] { "extrato-info" },
                UseBasicAuthentication = true
            });

            Assert.Equal("token-ficticio", await provider.GetAccessTokenAsync());
            Assert.Equal("Basic", handler.LastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("cliente-ficticio:segredo-ficticio")),
                handler.LastRequest.Headers.Authorization.Parameter);
            Assert.Equal("grant_type=client_credentials&scope=extrato-info", handler.LastRequestBody);
        }

        [Theory]
        [InlineData("segredo", "grant_type=client_credentials&client_id=cliente&client_secret=segredo&scope=escopo")]
        [InlineData(null, "grant_type=client_credentials&client_id=cliente&scope=escopo")]
        public async Task GetAccessTokenAsync_Padrao_PreservaCredenciaisNoCorpo(string? secret, string expectedBody)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"access_token\":\"token-ficticio\",\"expires_in\":3600}");
            var provider = new OAuthTokenProvider(new HttpClient(handler), new OAuthTokenProviderOptions
            {
                TokenEndpoint = new Uri("https://oauth.example.test/oauth/token"),
                ClientId = "cliente",
                ClientSecret = secret,
                Scopes = new[] { "escopo" }
            });

            await provider.GetAccessTokenAsync();

            Assert.Null(handler.LastRequest!.Headers.Authorization);
            Assert.Equal(expectedBody, handler.LastRequestBody);
        }
    }
}

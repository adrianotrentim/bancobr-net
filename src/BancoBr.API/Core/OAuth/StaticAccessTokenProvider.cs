using System;
using System.Threading;
using System.Threading.Tasks;

namespace BancoBr.API.Core.OAuth
{
    /// <summary>
    /// Usa um token Bearer fixo, fornecido externamente, em vez de obtê-lo via client_credentials.
    /// Útil quando o ambiente de sandbox fornece um "Access token (Bearer)" pronto no portal,
    /// sem expor o endpoint de token para o fluxo OAuth2 completo.
    /// </summary>
    public class StaticAccessTokenProvider : IAccessTokenProvider
    {
        private readonly string _accessToken;

        public StaticAccessTokenProvider(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("Access token não pode ser vazio.", nameof(accessToken));
            }

            _accessToken = accessToken;
        }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accessToken);
        }

        public void InvalidateToken()
        {
        }
    }
}

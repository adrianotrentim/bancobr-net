using System;
using System.Collections.Generic;

namespace BancoBr.API.Core.OAuth
{
    internal class OAuthTokenProviderOptions
    {
        /// <summary>
        /// Endpoint de emissão de token OAuth2 (client_credentials).
        /// </summary>
        public Uri TokenEndpoint { get; set; }

        public string ClientId { get; set; }

        /// <summary>
        /// Algumas APIs bancárias usam apenas o certificado mTLS para autenticar o client,
        /// sem client_secret. Quando nulo, o campo não é enviado no corpo do token request.
        /// </summary>
        public string ClientSecret { get; set; }

        public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    }
}

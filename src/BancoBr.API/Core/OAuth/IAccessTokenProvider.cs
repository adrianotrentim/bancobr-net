using System.Threading;
using System.Threading.Tasks;

namespace BancoBr.API.Core.OAuth
{
    /// <summary>
    /// Fornece o token Bearer usado nas chamadas autenticadas. Implementações típicas:
    /// <see cref="OAuthTokenProvider"/> (fluxo client_credentials real) e
    /// <see cref="StaticAccessTokenProvider"/> (token já emitido, ex.: o "Access token (Bearer)"
    /// que alguns portais de sandbox fornecem prontos para teste, sem expor um endpoint de token).
    /// </summary>
    public interface IAccessTokenProvider
    {
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Descarta o token em cache, forçando a próxima chamada a <see cref="GetAccessTokenAsync"/>
        /// a obter um novo. Usado quando a API rejeita o token atual (ex.: HTTP 401).
        /// </summary>
        void InvalidateToken();
    }
}

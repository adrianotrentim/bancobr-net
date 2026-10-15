using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes que gerenciam o "espaço de trabalho" onde o banco executa os
    /// pagamentos. Só o Santander tem esse conceito (workspace do Hub de Pagamentos); os demais
    /// bancos não implementam.
    /// </summary>
    internal abstract class WorkspaceApiBase
    {
        protected WorkspaceApiBase(HttpClient httpClient, Correntista origem)
        {
            HttpClient = httpClient;
            Origem = origem;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>Conta pagadora/titular informada em <c>ConfiguracaoAPI.Correntista</c>.</summary>
        protected Correntista Origem { get; }

        /// <summary>
        /// Devolve o id da workspace da conta pagadora (<c>Origem</c>), criando-a ou
        /// habilitando nela os produtos que faltam quando necessário.
        /// </summary>
        internal abstract Task<string> ObterOuCriarWorkspaceAsync(CancellationToken cancellationToken = default);
    }
}

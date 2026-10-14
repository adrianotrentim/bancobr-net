using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Http;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Pagamentos.Workspaces
{
    /// <summary>
    /// Cliente da API de workspaces do Hub de Pagamentos do Santander (management_payments_partners
    /// v1). É o único ponto que consulta/cria workspaces: devolve o id da workspace PAYMENTS ativa
    /// que contém a conta pagadora, com Pix, boleto, DDA, código de barras e TED habilitados — criando-a
    /// ou ligando os produtos que faltam. O ERP guarda o id e o informa em
    /// <c>BancoApi.Conectar</c>, para que os clientes de pagamento não precisem consultar o banco
    /// a cada instância.
    /// </summary>
    internal class WorkspaceClient : WorkspaceApiBase
    {
        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;

        internal WorkspaceClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint))
        {
        }

        internal WorkspaceClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, SantanderHttpGateway.PagamentosBaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public WorkspaceClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        internal override Task<string> ObterOuCriarWorkspaceAsync(Correntista origem, CancellationToken cancellationToken = default) =>
            _gateway.ObterOuCriarWorkspaceAsync(_baseUrl, origem, cancellationToken);
    }
}

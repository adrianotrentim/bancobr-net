using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes que obtêm o comprovante (PDF) de um pagamento já efetivado, para
    /// qualquer tipo de <see cref="MovimentoItem"/> suportado pelo banco. Links e etapas de
    /// geração e download são resolvidos internamente pelo cliente de cada banco.
    /// </summary>
    internal abstract class ComprovanteApiBase
    {
        protected ComprovanteApiBase(HttpClient httpClient, Correntista origem)
        {
            HttpClient = httpClient;
            Origem = origem;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>Conta pagadora/titular informada em <c>ConfiguracaoAPI.Correntista</c>.</summary>
        protected Correntista Origem { get; }

        /// <summary>
        /// Obtém os bytes do PDF do pagamento identificado por
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/> e preenche
        /// <see cref="Movimento.ArquivoComprovantePDF"/>. Retorna nulo se ainda não disponível.
        /// </summary>
        internal abstract Task<byte[]> ConsultarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken = default);
    }
}

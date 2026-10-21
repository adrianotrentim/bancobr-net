using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes que obtêm o comprovante (PDF) de um pagamento já efetivado, para
    /// qualquer tipo de <see cref="MovimentoItem"/> (Pix, TED, boleto, convênio). Só o Santander
    /// tem esse endpoint na API; no Sicoob o comprovante já vem na consulta do pagamento.
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
        /// Obtém o link do comprovante do pagamento já efetivado (id do pagamento em
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/>) e o aplica em
        /// <see cref="Movimento.LinkComprovanteBancoBr"/> e
        /// <see cref="Movimento.ValidadeLinkComprovanteBancoBr"/>.
        /// </summary>
        internal abstract Task<Movimento> ConsultarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken = default);
    }
}

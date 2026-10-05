using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamentos Pix (iniciação por chave DICT +
    /// confirmação), independente do banco — mesma ideia de
    /// <see cref="PagamentoConvenioApiBase"/>: cada banco herda e implementa os métodos
    /// abaixo de acordo com sua própria API.
    ///
    /// O contrato público é o mesmo <see cref="Movimento"/>/<see cref="MovimentoItem"/> usado
    /// pelo BancoBr.CNAB: cada método recebe o movimento, mapeia para o formato da API do
    /// banco, e aplica a resposta de volta no mesmo objeto — que é então devolvido (mesma
    /// instância, mutada in-place).
    /// </summary>
    internal abstract class PagamentoPixApiBase
    {
        protected PagamentoPixApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Consulta a situação de um pagamento já iniciado. O identificador do pagamento no
        /// banco (EndToEndId) é lido de <see cref="Movimento.NumeroDocumentoNoBanco"/>.
        /// </summary>
        internal abstract Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve a chave DICT informada em
        /// <see cref="MovimentoItemTransferenciaPIX.ChavePIX"/> para os dados do titular, sem
        /// movimentar valores. Espera um <see cref="MovimentoItemTransferenciaPIX"/> como
        /// <see cref="Movimento.MovimentoItem"/>.
        /// </summary>
        internal abstract Task<Movimento> IniciarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Efetiva um pagamento previamente iniciado por
        /// <see cref="IniciarPagamentoAsync"/>.
        /// </summary>
        internal abstract Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conveniência para o caso sem aprovação humana entre iniciação e confirmação: chama
        /// <see cref="IniciarPagamentoAsync"/> seguido de <see cref="ConfirmarPagamentoAsync"/>
        /// no mesmo movimento. Quando o ERP precisa exibir o titular resolvido para confirmação
        /// antes de pagar, use os dois métodos separadamente.
        /// </summary>
        internal abstract Task<Movimento> PagarComIniciacaoAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga um Pix Copia e Cola (QR Code estático ou com vencimento) com execução
        /// direta — ao contrário de <see cref="IniciarPagamentoAsync"/> +
        /// <see cref="ConfirmarPagamentoAsync"/>, não há passo de confirmação separado.
        /// Espera um <see cref="MovimentoItemPagamentoTituloPIXQRCode"/> como
        /// <see cref="Movimento.MovimentoItem"/>.
        /// </summary>
        internal abstract Task<Movimento> PagarViaQrCodeAsync(Correntista origem, Movimento movimento, CancellationToken cancellationToken = default);
    }
}

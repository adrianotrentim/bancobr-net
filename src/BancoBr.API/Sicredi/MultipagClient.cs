using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Sicredi
{
    /// <summary>Distribui as operações diretas Multipag para o cliente e os escopos de cada modalidade.</summary>
    internal sealed class MultipagClient : ComprovanteApiBase, IPagamentoDiretoApi
    {
        private readonly IReadOnlyDictionary<Type, PagamentosClient> _clientes;

        internal MultipagClient(PagamentosClient boleto, PagamentosClient tributo, PagamentosClient pix)
            : base(null, boleto.Gateway.Origem)
        {
            _clientes = new Dictionary<Type, PagamentosClient>
            {
                [typeof(MovimentoItemPagamentoTituloCodigoBarra)] = boleto,
                [typeof(MovimentoItemPagamentoConvenioCodigoBarra)] = tributo,
                [typeof(MovimentoItemTransferenciaPIX)] = pix
            };
        }

        private PagamentosClient ObterCliente(Movimento movimento)
        {
            if (movimento?.MovimentoItem == null) throw new ArgumentException("Informe o movimento e o item do pagamento.", nameof(movimento));
            foreach (var entrada in _clientes)
                if (entrada.Key.IsInstanceOfType(movimento.MovimentoItem)) return entrada.Value;
            throw new NotSupportedException("Multipag suporta boleto, tributo com barras e Pix por chave ou dados bancários.");
        }

        public Task<Movimento> EnviarAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken)
            => ObterCliente(movimento).PagarAsync(movimento, idempotencyKey, cancellationToken);

        public Task<Movimento> CancelarAsync(Movimento movimento, CancellationToken cancellationToken)
            => ObterCliente(movimento).CancelarAsync(movimento, cancellationToken);

        internal override async Task<bool> ConsultarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var cliente = ObterCliente(movimento);
            movimento.ArquivoComprovantePDF = null;
            await cliente.ConsultarAsync(movimento, cancellationToken).ConfigureAwait(false);
            if (movimento.SituacaoBancoBr != BancoBrSituacaoEnum.Efetivado) return false;

            var pdf = await cliente.ComprovanteAsync(movimento, cancellationToken).ConfigureAwait(false);
            movimento.ArquivoComprovantePDF = pdf;
            return true;
        }
    }
}

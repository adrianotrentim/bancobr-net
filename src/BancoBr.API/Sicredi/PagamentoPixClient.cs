using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.Common.Instances;

namespace BancoBr.API.Sicredi
{
    internal sealed class PagamentoPixClient : PagamentoPixApiBase
    {
        private readonly PagamentosClient _api;
        internal PagamentoPixClient(PagamentosClient api) : base(null, api.Gateway.Origem) { _api = api; }
        internal override Task<Movimento> ConsultarPagamentoAsync(Movimento m, CancellationToken cancellationToken = default) => _api.ConsultarAsync(m, cancellationToken);
        internal override Task<Movimento> IniciarPagamentoAsync(Movimento m, CancellationToken cancellationToken = default) => throw new NotSupportedException("Sicredi não oferece resolução prévia da chave Pix.");
        internal override Task<Movimento> ConfirmarPagamentoAsync(Movimento m, CancellationToken cancellationToken = default) => _api.ConsultarAsync(m, cancellationToken);
        internal override Task<Movimento> PagarComIniciacaoAsync(Movimento m, CancellationToken cancellationToken = default) => _api.PagarAsync(m, null, cancellationToken);
        internal override Task<Movimento> PagarViaQrCodeAsync(Movimento m, CancellationToken cancellationToken = default) => throw new NotSupportedException("Multipag não oferece pagamento por QR Code.");
    }
}

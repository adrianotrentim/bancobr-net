using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.Common.Instances;

namespace BancoBr.API.Sicredi
{
    internal sealed class PagamentoConvenioClient : PagamentoConvenioApiBase
    {
        private readonly PagamentosClient _api;
        internal PagamentoConvenioClient(PagamentosClient api) : base(null, api.Gateway.Origem) { _api = api; }
        internal override bool ExigeConsultaPrevia => false;
        internal override Task<Movimento> ConsultarCodigoBarrasAsync(Movimento m, bool? recebimentoViaCaixa = null, int unidade = 0, CancellationToken cancellationToken = default) => throw new NotSupportedException("Sicredi não oferece consulta prévia do código de barras.");
        internal override Task<Movimento> PagarConvenioAsync(Movimento m, int unidade = 0, CancellationToken cancellationToken = default) => _api.PagarAsync(m, null, cancellationToken);
        internal override Task<Movimento> ConsultarComprovantePorNsuAsync(Movimento m, CancellationToken cancellationToken = default) => _api.ConsultarAsync(m, cancellationToken);
        internal override Task<IReadOnlyList<Movimento>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        internal override Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        internal override Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}

using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>Consulta do saldo atual da conta, representado por MovimentoItemExtratoSaldo.</summary>
    internal interface ISaldoApi
    {
        Task<Movimento> ConsultarSaldoAsync(CancellationToken cancellationToken);
    }
}

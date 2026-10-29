using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>Pagamento sem consulta/iniciação prévia, com identidade preservada entre tentativas.</summary>
    internal interface IPagamentoDiretoApi
    {
        Task<Movimento> EnviarAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken);
        Task<Movimento> CancelarAsync(Movimento movimento, CancellationToken cancellationToken);
    }
}

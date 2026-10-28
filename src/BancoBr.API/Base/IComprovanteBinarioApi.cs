using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>Download do comprovante em PDF binário, sem conversão do conteúdo para texto.</summary>
    internal interface IComprovanteBinarioApi
    {
        Task<byte[]> BaixarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken);
    }
}

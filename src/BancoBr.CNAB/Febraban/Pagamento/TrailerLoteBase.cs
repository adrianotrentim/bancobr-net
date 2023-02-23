using System.Collections.Generic;
using BancoBr.CNAB.Base;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class TrailerLoteBase : Base.TrailerLoteBase
    {
        public TrailerLoteBase(Lote lote, List<RegistroBase> registros)
            : base(lote, registros)
        {
        }
    }
}
using System.Collections.Generic;
using BancoBr.CNAB.Base;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class TrailerLote : Base.TrailerLote
    {
        public TrailerLote(Lote lote, List<Registro> registros)
            : base(lote, registros)
        {
        }
    }
}
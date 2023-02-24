using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Base
{
    public class TrailerLoteBase : RegistroBase
    {
        public Lote Lote { get; }

        public TrailerLoteBase(Lote lote)
            : base(lote.Header.Banco)
        {
            TipoRegistro = 5;

            Lote = lote;
        }

        [CampoCNAB(2, 4)] 
        public new int LoteServico => Lote.Header.LoteServico;

        [CampoCNAB(4, 9)]
        public int CNAB1 { get; set; }

        [CampoCNAB(5, 6)]
        public int QuantidadeRegistros => Lote.Registros.Count + 2; //2 = Header de Lote + Trailer de Lote
    }
}
using System.Collections.Generic;
using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Santander
{
    public class TrailerArquivo : Febraban.TrailerArquivo
    {
        public TrailerArquivo(ArquivoCNAB cnab, List<Lote> lotes)
            : base(cnab, lotes)
        {
        }

        [CampoCNAB(true)]
        private new int QuantidadeContasConciliacao => 0;

        [CampoCNAB(30, 6)]
        public string CNAB3 => "";
    }
}
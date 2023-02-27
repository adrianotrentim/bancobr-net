using System.Collections.Generic;
using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Itau
{
    public class TrailerArquivo : Febraban.TrailerArquivo
    {
        public TrailerArquivo(ArquivoCNAB cnab, List<Lote> lotes)
            : base(cnab, lotes)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new int QuantidadeContasConciliacao { get; set; }

        #endregion

        [CampoCNAB(30, 211)]
        public new string CNAB2 { get; set; }
    }
}
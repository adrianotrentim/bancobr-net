using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.CNAB.Itau
{
    public class SegmentoJ : Febraban.SegmentoJ
    {
        public SegmentoJ(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(true)]
        private new int CodigoMoeda { get; set; }

        [CampoCNAB(true)]
        private new string CNAB1 { get; set; }

        #endregion

        [CampoCNAB(168, 15)]
        public decimal CNAB2 { get; set; }

        [CampoCNAB(203, 13)]
        public string CNAB3 { get; set; }

        [CampoCNAB(216, 15)]
        public override string NumeroDocumentoBanco { get; set; }
    }
}

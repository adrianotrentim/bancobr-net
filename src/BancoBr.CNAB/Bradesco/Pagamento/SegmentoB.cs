using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco.Pagamento
{
    public class SegmentoB : Febraban.Pagamento.SegmentoB
    {
        public SegmentoB(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "B";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string Informacao12 { get; set; }

        [CampoCNAB(true)]
        private new int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(true)]
        private new int IdentificacaoBancoSPB { get; set; }

        #endregion

        [CampoCNAB(17, 8)]
        public DateTime Vencimento { get; set; }

        [CampoCNAB(18, 15)]
        public decimal ValorDocumento { get; set; }

        [CampoCNAB(19, 15)]
        public decimal Abatimento { get; set; }

        [CampoCNAB(20, 15)]
        public decimal Desconto { get; set; }

        [CampoCNAB(21, 15)]
        public decimal Mora { get; set; }

        [CampoCNAB(22, 15)]
        public decimal Multa { get; set; }

        [CampoCNAB(23, 15)]
        public string DocumentoFavorecido { get; set; }

        [CampoCNAB(24, 15)]
        public string CNAB2 { get; set; }
    }
}
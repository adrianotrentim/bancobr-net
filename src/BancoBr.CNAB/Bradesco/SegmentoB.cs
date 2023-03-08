using System;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Bradesco
{
    public class SegmentoB : Febraban.SegmentoB
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

        #endregion

        [CampoCNAB(128, 8)]
        public DateTime Vencimento { get; set; }

        [CampoCNAB(136, 15)]
        public decimal ValorDocumento { get; set; }

        [CampoCNAB(151, 15)]
        public decimal Abatimento { get; set; }

        [CampoCNAB(166, 15)]
        public decimal Desconto { get; set; }

        [CampoCNAB(181, 15)]
        public decimal Mora { get; set; }

        [CampoCNAB(196, 15)]
        public decimal Multa { get; set; }

        [CampoCNAB(211, 15)]
        public string DocumentoFavorecido { get; set; }

        [CampoCNAB(226, 1)]
        public int AvisoFavorecido { get; set; }

        [CampoCNAB(227, 6)]
        public override int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(233, 8)]
        public override int IdentificacaoBancoSPB { get; set; }
    }
}
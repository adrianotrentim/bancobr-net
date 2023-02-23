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
        private new IdentificacaoFavorecidoEnum IdentificacaoFavorecido { get; set; }

        [CampoCNAB(true)]
        private new string Informacao10 { get; set; }

        [CampoCNAB(true)]
        private new string Informacao11 { get; set; }

        [CampoCNAB(true)]
        private new string Informacao12 { get; set; }

        [CampoCNAB(true)]
        private new int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(true)]
        private new int IdentificacaoBancoSPB { get; set; }

        #endregion

        [CampoCNAB(6, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(7, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoFavorecido { get; set; }

        [CampoCNAB(8, 14)]
        public int InscricaoFavorecido { get; set; }

        [CampoCNAB(9, 30)]
        public string EnderecoFavorecido { get; set; }

        [CampoCNAB(10, 5)]
        public string NumeroEnderecoFavorecido { get; set; }

        [CampoCNAB(11, 15)]
        public string ComplementoEnderecoFavorecido { get; set; }

        [CampoCNAB(12, 15)]
        public string BairroFavorecido { get; set; }

        [CampoCNAB(13, 20)]
        public string CidadeFavorecido { get; set; }

        [CampoCNAB(14, 8)]
        public int CEPFavorecido { get; set; }

        [CampoCNAB(16, 2)]
        public string UFFavorecido { get; set; }

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
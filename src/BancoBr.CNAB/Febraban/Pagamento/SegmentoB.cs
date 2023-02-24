using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class SegmentoB : RegistroDetalheBase
    {
        public SegmentoB(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "B";
        }

        [CampoCNAB(6, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(7, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoFavorecido { get; set; }

        [CampoCNAB(8, 14)]
        public long InscricaoFavorecido { get; set; }

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

        [CampoCNAB(17, 99)]
        public string Informacao12 { get; set; }

        [CampoCNAB(18, 6)]
        public int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(19, 8)]
        public int IdentificacaoBancoSPB { get; set; }
    }
}
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

        [CampoCNAB(15, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(18, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoFavorecido { get; set; }

        [CampoCNAB(19, 14)]
        public long InscricaoFavorecido { get; set; }

        [CampoCNAB(33, 30)]
        public string EnderecoFavorecido { get; set; }

        [CampoCNAB(63, 5)]
        public string NumeroEnderecoFavorecido { get; set; }

        [CampoCNAB(68, 15)]
        public string ComplementoEnderecoFavorecido { get; set; }

        [CampoCNAB(83, 15)]
        public string BairroFavorecido { get; set; }

        [CampoCNAB(98, 20)]
        public string CidadeFavorecido { get; set; }

        [CampoCNAB(118, 8)]
        public int CEPFavorecido { get; set; }

        [CampoCNAB(126, 2)]
        public string UFFavorecido { get; set; }

        [CampoCNAB(128, 99)]
        public string Informacao12 { get; set; }

        [CampoCNAB(211, 6)]
        public int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(226, 8)]
        public int IdentificacaoBancoSPB { get; set; }
    }
}
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
        public IdentificacaoFavorecidoEnum IdentificacaoFavorecido { get; set; }

        [CampoCNAB(7, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoFavorecido { get; set; }

        [CampoCNAB(8, 14)]
        public int InscricaoFavorecido { get; set; }

        [CampoCNAB(9, 35)]
        public string Informacao10 { get; set; }

        [CampoCNAB(10, 60)]
        public string Informacao11 { get; set; }

        [CampoCNAB(11, 99)]
        public string Informacao12 { get; set; }

        [CampoCNAB(12, 6)]
        public int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(13, 8)]
        public int IdentificacaoBancoSPB { get; set; }
    }
}
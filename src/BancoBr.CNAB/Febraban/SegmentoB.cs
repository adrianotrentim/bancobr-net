using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;
using BancoBr.CNAB.Base;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoB : RegistroDetalheBase
    {
        public SegmentoB(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "B";
        }

        [CampoCNAB(18, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoFavorecido { get; set; }

        [CampoCNAB(19, 14)]
        public virtual long InscricaoFavorecido { get; set; }

        [CampoCNAB(227, 6)]
        public int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(233, 8)]
        public int IdentificacaoBancoSPB { get; set; }
    }
}

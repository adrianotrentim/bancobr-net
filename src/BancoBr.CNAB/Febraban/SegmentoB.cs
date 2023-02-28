using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

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

        [CampoCNAB(15, 3)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(18, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoFavorecido { get; set; }

        [CampoCNAB(19, 14)]
        public virtual long InscricaoFavorecido { get; set; }

        [CampoCNAB(33, 30)]
        public virtual string EnderecoFavorecido { get; set; }

        [CampoCNAB(63, 5)]
        public virtual string NumeroEnderecoFavorecido { get; set; }

        [CampoCNAB(68, 15)]
        public virtual string ComplementoEnderecoFavorecido { get; set; }

        [CampoCNAB(83, 15)]
        public virtual string BairroFavorecido { get; set; }

        [CampoCNAB(98, 20)]
        public virtual string CidadeFavorecido { get; set; }

        [CampoCNAB(118, 8)]
        public virtual int CEPFavorecido { get; set; }

        [CampoCNAB(126, 2)]
        public virtual string UFFavorecido { get; set; }

        [CampoCNAB(128, 99)]
        public virtual string Informacao12 { get; set; }

        [CampoCNAB(211, 6)]
        public virtual int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(226, 8)]
        public virtual int IdentificacaoBancoSPB { get; set; }
    }
}
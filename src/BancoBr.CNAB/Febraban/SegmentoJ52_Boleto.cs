using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoJ52_Boleto : RegistroDetalheBase
    {
        public SegmentoJ52_Boleto(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "J";
            CodigoRegistroOpcional = 52;
        }

        [CampoCNAB(15, 1)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(16, 2)]
        public virtual int CodigoMovimentoRemessa { get; set; }

        [CampoCNAB(18, 2)] 
        public virtual int CodigoRegistroOpcional { get; set; }

        [CampoCNAB(20, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoSacado { get; set; }

        [CampoCNAB(21, 15)]
        public virtual long InscricaoSacado { get; set; }

        [CampoCNAB(36, 40)]
        public virtual string NomeSacado { get; set; }

        [CampoCNAB(76, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoCedente { get; set; }

        [CampoCNAB(77, 15)]
        public virtual long InscricaoCedente { get; set; }

        [CampoCNAB(92, 40)]
        public virtual string NomeCedente { get; set; }

        [CampoCNAB(132, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoSacadorAvalista { get; set; }

        [CampoCNAB(133, 15)]
        public virtual long InscricaoSacadorAvalista { get; set; }

        [CampoCNAB(148, 40)]
        public virtual string NomeSacadorAvalista { get; set; }

        [CampoCNAB(188, 53)]
        public virtual string CNAB2 { get; set; }
    }
}
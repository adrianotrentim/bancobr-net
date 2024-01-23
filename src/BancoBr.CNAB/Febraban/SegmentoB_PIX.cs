using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;
using Banco = BancoBr.Common.Instances.Banco;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoB_PIX : SegmentoB
    {
        public SegmentoB_PIX(Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(15, 3)]
        public virtual FormaIniciacaoEnum FormaIniciacao { get; set; }

        [CampoCNAB(33, 35)]
        public virtual string TX_ID { get; set; }

        [CampoCNAB(68, 60)]
        public virtual string InformacaoEntreUsuarios { get; set; }

        [CampoCNAB(128, 99)]
        public virtual string ChavePIX { get; set; }
    }
}
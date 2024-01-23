using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoA_Transferencia : SegmentoA
    {
        public SegmentoA_Transferencia(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(21, 3)]
        public virtual int BancoFavorecido { get; set; }

        [CampoCNAB(24, 5)]
        public virtual int AgenciaFavorecido { get; set; }

        [CampoCNAB(29, 1)]
        public virtual string DVAgenciaFavorecido { get; set; }

        [CampoCNAB(30, 12)]
        public virtual int ContaFavorecido { get; set; }

        [CampoCNAB(42, 1)]
        public virtual string DVContaFavorecido { get; set; }

        [CampoCNAB(43, 1)]
        public virtual string DVAgenciaContaFavorecido { get; set; }

        /// <summary>
        /// Verificar Nota P011 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(220, 5)]
        public FinalidadeTEDEnum CodigoFinalidadeTED { get; set; }
    }
}
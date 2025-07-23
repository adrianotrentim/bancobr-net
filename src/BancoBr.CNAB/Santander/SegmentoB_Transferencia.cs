using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Santander
{
    public class SegmentoB_Transferencia : Febraban.SegmentoB_Transferencia
    {
        public SegmentoB_Transferencia(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new int CodigoUGCentralizadora { get; set; }

        #endregion

        [CampoCNAB(226, 4)]
        public int CodigoHistoricoParaCredito { get; set; }

        [CampoCNAB(230, 1)]
        public new int AvisoFavorecido { get; set; }
        
        [CampoCNAB(231, 1)]
        public string Filler { get; set; }

        [CampoCNAB(232, 1)]
        public string TEDParaInstituicaoFinanceira { get; set; }
    }
}
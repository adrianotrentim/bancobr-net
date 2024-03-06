using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Itau
{
    public class SegmentoJ52_Boleto : Febraban.SegmentoJ52_Boleto
    {
        public SegmentoJ52_Boleto(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string CNAB1 { get; set; }

        [CampoCNAB(true)]
        private new int CodigoMovimentoRemessa { get; set; }

        #endregion

        [CampoCNAB(15, 1)]
        public TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(16, 2)]
        public CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }
    }
}
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Itau
{
    public class TrailerLote : Febraban.TrailerLote
    {
        public TrailerLote(Lote lote)
            : base(lote)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(true)]
        private new int NumeroAvisoDebito { get; set; }

        #endregion

        [CampoCNAB(42, 18)]
        public decimal ZEROS1 { get; set; }

        [CampoCNAB(66, 171)]
        public override string CNAB2 { get; set; }
    }
}
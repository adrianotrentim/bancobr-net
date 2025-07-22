using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Inter
{
    public sealed class HeaderLote : Febraban.HeaderLote_TransferenciaConvenio
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new LocalDebitoEnum LocalDebito { get; set; }

        #endregion

        [CampoCNAB(223, 2)]
        public string IndicativoLocalDebito { get; set; }
    }
}

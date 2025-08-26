using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Santander
{
    public sealed class HeaderLote : Febraban.HeaderLote_TransferenciaConvenio
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 31;
            Operacao = "C";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new LocalDebitoEnum LocalDebito { get; set; }

        #endregion

        [CampoCNAB(223, 8)]
        public override string CNAB2 { get; set; }
    }
}

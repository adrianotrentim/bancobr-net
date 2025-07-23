using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Inter
{
    public sealed class HeaderLote : Febraban.HeaderLote_TransferenciaConvenio
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(223, 2)]
        public new string LocalDebito { get; set; }
    }
}

using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco
{
    public class HeaderLote : Febraban.HeaderLote
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 45;
            Operacao = "C";
        }
    }
}

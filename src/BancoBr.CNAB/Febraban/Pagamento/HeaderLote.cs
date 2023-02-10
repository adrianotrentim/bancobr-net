using System;
using System.Text;
using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class HeaderLote : Base.HeaderLote
    {
        public HeaderLote(Common.Instances.Banco banco) 
            : base(banco)
        {
        }
    }
}

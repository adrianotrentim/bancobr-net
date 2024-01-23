using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco
{
    public class HeaderLote_PagamentoTransferencia : Febraban.HeaderLote_PagamentoTransferencia
    {
        public HeaderLote_PagamentoTransferencia(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 45;
            Operacao = "C";
        }
    }
}

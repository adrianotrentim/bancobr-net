/*
 * Pagamento de Títulos de Cobrança
 */

namespace BancoBr.CNAB.Febraban
{
    public sealed class HeaderLote_PagamentoTitulo : HeaderLote
    {
        public HeaderLote_PagamentoTitulo(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 40;
            Operacao = "C";
        }
    }
}

namespace BancoBr.CNAB.Febraban
{
    public sealed class HeaderLote_PagamentoConvenio : HeaderLote
    {
        public HeaderLote_PagamentoConvenio(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 12;
            Operacao = "C";
        }
    }
}
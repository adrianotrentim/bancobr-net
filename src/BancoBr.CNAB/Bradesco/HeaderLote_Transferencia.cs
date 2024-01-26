namespace BancoBr.CNAB.Bradesco
{
    public class HeaderLote_Transferencia : Febraban.HeaderLote_Transferencia
    {
        public HeaderLote_Transferencia(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 45;
            Operacao = "C";
        }
    }
}

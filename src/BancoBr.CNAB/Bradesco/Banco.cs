using BancoBr.CNAB.Febraban;
using BancoBr.Common.Interfaces;

namespace BancoBr.CNAB.Bradesco
{
    public class Banco : Febraban.Banco
    {
        public Banco()
        {
            Codigo = 237;
            Nome = "Banco Bradesco SA";
        }

        public override string ToString()
        {
            return $"[({Codigo}) {Nome}";
        }
    }
}

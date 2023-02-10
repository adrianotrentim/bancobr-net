using BancoBr.CNAB.Febraban;
using BancoBr.Common.Interfaces;

namespace BancoBr.CNAB.Santander
{
    public class Banco : Febraban.Banco, IBanco
    {
        public Banco()
        {
            Codigo = 33;
            Nome = "Banco Santander";
        }

        public override string ToString()
        {
            return $"[({Codigo}) {Nome}";
        }
    }
}

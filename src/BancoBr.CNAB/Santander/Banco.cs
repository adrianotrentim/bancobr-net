using BancoBr.CNAB.Febraban;
using BancoBr.Common.Interfaces;

namespace BancoBr.CNAB.Santander
{
    public class Banco : Febraban.Banco
    {
        public Banco()
            : base(33, "Banco Santander")
        {
        }
    }
}

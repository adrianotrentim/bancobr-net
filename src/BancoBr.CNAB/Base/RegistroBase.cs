using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Base
{
    public abstract class RegistroBase : IRegistro
    {
        public Common.Instances.Banco Banco { get; }

        protected RegistroBase(Common.Instances.Banco banco)
        {
            Banco = banco;
        }

        [CampoCNAB(1, 3)]
        public int CodigoBanco => Banco.Codigo;

        [CampoCNAB(2, 4)]
        public int LoteServico { get; set; }

        [CampoCNAB(3, 1)]
        public int TipoRegistro { get; set; }
    }
}

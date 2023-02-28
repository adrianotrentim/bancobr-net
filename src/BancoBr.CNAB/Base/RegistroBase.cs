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
            CodigoBanco = Banco.Codigo;
        }

        [CampoCNAB(1, 3)]
        public virtual int CodigoBanco { get; set; }

        [CampoCNAB(4, 4)]
        public virtual int LoteServico { get; set; }

        [CampoCNAB(8, 1)]
        public virtual int TipoRegistro { get; set; }
    }
}

using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Base
{
    public class RegistroDetalheBase : RegistroBase
    {
        protected RegistroDetalheBase(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
        }

        [CampoCNAB(9, 5)]
        public virtual int NumeroRegistro { get; set; }

        [CampoCNAB(14, 1)]
        public virtual string CodigoSegmento { get; set; }
    }
}

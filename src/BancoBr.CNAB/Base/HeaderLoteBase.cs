using BancoBr.Common.Attributes;
using System.Collections.Generic;

namespace BancoBr.CNAB.Base
{
    public class HeaderLoteBase : RegistroBase
    {
        protected HeaderLoteBase(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 1;
        }

        [CampoCNAB(3, 1)]
        public string Operacao { get; set; }

        [CampoCNAB(4, 2)]
        public int Servico { get; set; }

        public List<RegistroBase> Registros { get; set; }
    }
}

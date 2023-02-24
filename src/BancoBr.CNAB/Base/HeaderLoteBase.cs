using BancoBr.Common.Attributes;
using System.Collections.Generic;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Base
{
    public class HeaderLoteBase : RegistroBase
    {
        protected HeaderLoteBase(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 1;
        }

        [CampoCNAB(4, 1)]
        public string Operacao { get; set; }

        [CampoCNAB(5, 2)]
        public TipoServicoEnum Servico { get; set; }

        public List<RegistroBase> Registros { get; set; }
    }
}

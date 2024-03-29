﻿using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Base
{
    public abstract class HeaderLoteBase : RegistroBase
    {
        protected HeaderLoteBase(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 1;
        }

        [CampoCNAB(9, 1)]
        public virtual string Operacao { get; set; }

        [CampoCNAB(10, 2)]
        public virtual TipoServicoEnum Servico { get; set; }
    }
}

using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.Common.Interfaces.CNAB
{
    public interface IRegistro
    {
        [CNAB(1, 3)]
        int CodigoBanco { get; set; }

        [CNAB(2, 4)]
        int LoteServico { get; set; }

        [CNAB(3, 1)]
        int TipoRegistro { get; set; }
    }
}

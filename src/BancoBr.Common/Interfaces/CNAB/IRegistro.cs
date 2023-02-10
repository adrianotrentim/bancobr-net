using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Instances;

namespace BancoBr.Common.Interfaces.CNAB
{
    public interface IRegistro
    {
        int CodigoBanco { get; }
        int LoteServico { get; set; }
        int TipoRegistro { get; set; }
    }
}

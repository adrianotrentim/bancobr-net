using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Febraban
{
    public class Registro : IRegistro
    {
        public int CodigoBanco { get; set; }
        public int LoteServico { get; set; }
        public int TipoRegistro { get; set; }
    }
}

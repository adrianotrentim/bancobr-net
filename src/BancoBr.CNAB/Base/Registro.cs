using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Base
{
    public class Registro : IRegistro
    {
        public Common.Instances.Banco Banco { get; }

        public Registro(Common.Instances.Banco banco)
        {
            Banco = banco;
        }

        [CNAB(1, 3)]
        public int CodigoBanco => Banco.Codigo;

        [CNAB(2, 4)]
        public int LoteServico { get; set; }

        [CNAB(3, 1)]
        public int TipoRegistro { get; set; }
    }
}

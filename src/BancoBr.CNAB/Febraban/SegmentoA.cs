using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoA : Registro
    {
        public SegmentoA()
        {
            TipoRegistro = 3;
            CodigoSegmento = "A";
        }

        [CNAB(3, 5)]
        public int NumeroRegistro { get; set; }

        [CNAB(4, 1)]
        public string CodigoSegmento { get; }

    }
}

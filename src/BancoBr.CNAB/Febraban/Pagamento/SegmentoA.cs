using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class SegmentoA : Registro
    {
        public SegmentoA(Common.Instances.Banco banco) 
            : base(banco)
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

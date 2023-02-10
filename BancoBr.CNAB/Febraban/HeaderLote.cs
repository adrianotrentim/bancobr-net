using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Attributes;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Febraban
{
    public class HeaderLote : Registro
    {
        public HeaderLote()
        {
            TipoRegistro = 1;
        }

        [CNAB(3, 1)] 
        public string Operacao { get; set; }

        public List<SegmentoA> SegmentoA { get; set; }
    }
}

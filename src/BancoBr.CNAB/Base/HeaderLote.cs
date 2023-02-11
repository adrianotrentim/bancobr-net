using BancoBr.Common.Attributes;
using System.Collections.Generic;

namespace BancoBr.CNAB.Base
{
    public class HeaderLote : Registro
    {
        protected HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 1;
        }

        [CampoCNAB(3, 1)]
        public string Operacao { get; set; }

        [CampoCNAB(4, 2)]
        public int Servico { get; set; }

        public List<Registro> Registros { get; set; }
    }
}

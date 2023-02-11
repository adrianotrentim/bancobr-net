using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Febraban.PagamentoTitulo
{
    public class SegmentoA : Registro
    {
        public SegmentoA(Common.Instances.Banco banco) 
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "A";
        }

        [CampoCNAB(3, 5)]
        public int NumeroRegistro { get; set; }

        [CampoCNAB(4, 1)]
        public string CodigoSegmento { get; }

    }
}

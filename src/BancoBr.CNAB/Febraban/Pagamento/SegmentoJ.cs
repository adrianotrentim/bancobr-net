using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class SegmentoJ : RegistroDetalheBase
    {
        public SegmentoJ(Common.Instances.Banco banco) 
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "J";
        }
    }
}

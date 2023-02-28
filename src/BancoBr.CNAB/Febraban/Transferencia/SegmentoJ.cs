using BancoBr.CNAB.Base;

namespace BancoBr.CNAB.Febraban.Transferencia
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

using BancoBr.CNAB.Base;
using BancoBr.CNAB.Bradesco.Transferencia;

namespace BancoBr.CNAB.Bradesco
{
    public sealed class Banco : Base.Banco
    {
        public Banco()
            : base(237, "Banco Bradesco SA", 80)
        {
        }

        #region ::. Bloco de Transferências .::

        public override HeaderLoteBase NovoHeaderLote() => new HeaderLote(this);
        public override RegistroDetalheBase NovoSegmentoA() => new SegmentoA(this);
        public override RegistroDetalheBase NovoSegmentoB() => new SegmentoB(this);

        public override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Common.Instances.Transferencia transferencia) => null;
        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Transferencia transferencia) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Transferencia transferencia) => null;

        #endregion
    }
}

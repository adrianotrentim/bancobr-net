using BancoBr.CNAB.Base;
using BancoBr.CNAB.Bradesco.Pagamento;

namespace BancoBr.CNAB.Bradesco
{
    public sealed class Banco : Base.Banco
    {
        public Banco()
            : base(237, "Banco Bradesco SA", 80)
        {
        }

        #region ::. Bloco de Pagamentos .::

        public override HeaderLoteBase NovoHeaderLote() => new HeaderLote(this);
        public override RegistroDetalheBase NovoSegmentoA() => new SegmentoA(this);

        public override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Common.Instances.Pagamento pagamento) => null;
        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Pagamento pagamento) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Pagamento pagamento) => null;

        #endregion
    }
}

using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco
{
    public sealed class Banco : Base.Banco
    {
        public Banco()
            : base(237, "Banco Bradesco SA", 80)
        {
        }

        public override HeaderLoteBase NovoHeaderLote(FormaPagamentoEnum formaPagamento) => new HeaderLote(this);
        public override RegistroDetalheBase NovoSegmentoA(FormaPagamentoEnum formaPagamento) => new SegmentoA(this);
        public override RegistroDetalheBase NovoSegmentoB(FormaPagamentoEnum formaPagamento) => new SegmentoB(this);

        public override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
    }
}

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

        public override HeaderLoteBase NovoHeaderLote(FormaLancamentoEnum formaLancamento) => new HeaderLote(this);
        public override RegistroDetalheBase NovoSegmentoB(FormaLancamentoEnum formaLancamento) => new SegmentoB(this);

        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
    }
}

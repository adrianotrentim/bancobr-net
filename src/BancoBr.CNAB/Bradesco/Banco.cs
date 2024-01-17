using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco
{
    public sealed class Banco : Base.Banco
    {
        public Banco()
            : base(237, "BRADESCO", 89)
        {
        }

        public override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento) => new HeaderLote(this);
        public override RegistroDetalheBase NovoSegmentoB(TipoLancamentoEnum tipoLancamento) => new SegmentoB(this);

        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
    }
}

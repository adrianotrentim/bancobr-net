using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System.Collections.Generic;

namespace BancoBr.CNAB.Itau
{
    public class Banco : Base.Banco
    {
        public Banco()
            : base(341, "Banco Itaú", 80)
        {
        }

        public override RegistroBase NovoHeaderArquivo(Pessoa empresaCedente, int numeroRemessa) => new HeaderArquivo(this, empresaCedente, numeroRemessa);
        public override RegistroBase NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Bloco de Transferências .::

        public override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.PIXTransferencia:
                    return new HeaderLote_TedPixCredConta(this);
                default:
                    throw new InvalidOperationException("Não Implementado");
            }
        }

        public override TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        //public override RegistroDetalheBase NovoSegmentoA() => new SegmentoA(this);
        //public override RegistroDetalheBase NovoSegmentoB() => new SegmentoB(this);

        public override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;

        #endregion
    }
}

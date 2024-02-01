using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System.Collections.Generic;

namespace BancoBr.CNAB.Itau
{
    public class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 341, "Banco Itaú", 80)
        {
        }

        internal override Febraban.HeaderArquivo NovoHeaderArquivo(int numeroRemessa, List<Movimento> movimentos) => new HeaderArquivo(this);
        internal override Febraban.TrailerArquivo NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Bloco de Transferências .::

        internal override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.PIXTransferencia:
                    return new HeaderLote_TED(this);
                default:
                    throw new InvalidOperationException("Não Implementado");
            }
        }

        internal override TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        //public override RegistroDetalheBase NovoSegmentoA() => new SegmentoA(this);
        //public override RegistroDetalheBase NovoSegmentoB() => new SegmentoB_DadosBancarios(this);

        internal override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        internal override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
        internal override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;

        #endregion
    }
}

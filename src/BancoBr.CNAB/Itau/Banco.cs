using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System.Collections.Generic;
using BancoBr.CNAB.Febraban;

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

        internal override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento) => new HeaderLote(this);

        internal override RegistroDetalheBase NovoSegmentoA(TipoLancamentoEnum tipoLancamento) => new SegmentoA(this);

        internal override RegistroDetalheBase NovoSegmentoB(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    return new SegmentoB_Transferencia(this);
                case TipoLancamentoEnum.PIXTransferencia:
                    return new SegmentoB_PIX(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        internal override RegistroDetalheBase NovoSegmentoJ(TipoLancamentoEnum tipoLancamento) => new SegmentoJ(this);

        internal override RegistroDetalheBase NovoSegmentoJ52(TipoLancamentoEnum tipoLancamento) => new SegmentoJ52_Boleto(this);

        internal override RegistroDetalheBase NovoSegmentoO(TipoLancamentoEnum tipoLancamento) => new SegmentoO(this);

        internal override TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        internal override HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote, TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.PIXTransferencia:
                    ((HeaderLote)headerLote).VersaoLote = 40;
                    break;
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    ((HeaderLote)headerLote).VersaoLote = 30;
                    break;
                case TipoLancamentoEnum.PagamentoTributosCodigoBarra:
                    ((HeaderLote)headerLote).VersaoLote = 30;
                    break;
            }

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoA transferencia)
            {
                transferencia.DVAgenciaFavorecido = "";
                transferencia.DVContaFavorecido = "";

                if (movimento.MovimentoItem is MovimentoItemTransferenciaPIX)
                {
                    transferencia.IdentificacaoTransferencia = "04";
                } 
                else if (movimento.MovimentoItem is MovimentoItemTransferenciaTED movimentoTED)
                {
                    transferencia.IdentificacaoTransferencia = movimentoTED.TipoConta == TipoContaEnum.ContaCorrente ? "01" : "03";
                }
            }

            return segmento;
        }

        internal override RegistroDetalheBase PreencheSegmentoJ52(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoJ52_Boleto segmentoJ52Boleto)
            {
                segmentoJ52Boleto.TipoMovimento = movimento.TipoMovimento;
                segmentoJ52Boleto.CodigoInstrucaoMovimento = movimento.CodigoInstrucao;
            }

            return segmento;
        }
    }
}

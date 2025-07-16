using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System.Collections.Generic;
using BancoBr.Common.Core;

namespace BancoBr.CNAB.Itau
{
    public class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 341, "Banco Itaú", 80)
        {
        }

        #region ::. Instancias .::

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

        internal override RegistroDetalheBase NovoSegmentoJ52(TipoLancamentoEnum tipoLancamento)
        {
            if (tipoLancamento == TipoLancamentoEnum.PIXQrCode)
                return new SegmentoJ52_PIX_QRCODE(this);

            return new SegmentoJ52_Boleto(this);
        }

        internal override RegistroDetalheBase NovoSegmentoO(TipoLancamentoEnum tipoLancamento) => new SegmentoO(this);

        internal override TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        #endregion

        #region ::. Preenchimento .::

        internal override Febraban.HeaderArquivo PreencheHeaderArquivo(Febraban.HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            headerArquivo.DVAgenciaConta = Empresa.DVConta.Substring(0, 1);

            return headerArquivo;
        }

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

            ((HeaderLote)headerLote).DVAgenciaConta = Empresa.DVConta.Substring(0, 1);

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoA transferencia)
            {
                transferencia.DVAgenciaFavorecido = "";
                transferencia.DVContaFavorecido = "";
                transferencia.InscricaoFavorecido = long.Parse(movimento.Favorecido.CPF_CNPJ.JustNumbers());
                
                if (movimento.MovimentoItem is MovimentoItemTransferenciaPIX pix)
                {
                    transferencia.IdentificacaoTransferencia = "04";
                }
                else if (movimento.MovimentoItem is MovimentoItemTransferenciaTED movimentoTED)
                {
                    transferencia.IdentificacaoTransferencia = movimentoTED.TipoConta == TipoContaEnum.ContaCorrente ? "01" : "03";
                    transferencia.CamaraCentralizadora = 0; // NOTA 35;
                    transferencia.DVContaFavorecido = "";
                    transferencia.DVAgenciaContaFavorecido = "";

                    if (movimentoTED.DVConta?.Length == 2)
                    {
                        transferencia.DVContaFavorecido = movimentoTED.DVConta.Substring(0, 1);
                        transferencia.DVAgenciaContaFavorecido = movimentoTED.DVConta.Substring(1, 1);
                    } 
                    else if (movimentoTED.DVConta?.Length == 1)
                    {
                        transferencia.DVContaFavorecido = "";
                        transferencia.DVAgenciaContaFavorecido = movimentoTED.DVConta.Substring(0, 1);
                    }
                }

                if (transferencia.CodigoInstrucaoMovimento == CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado)
                    transferencia.CodigoInstrucaoMovimento = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado;
            }

            return segmento;
        }

        internal override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoB_PIX pix)
            {
                if (pix.FormaIniciacao == FormaIniciacaoEnum.PIX_Telefone)
                    if (!pix.ChavePIX.Contains("+55"))
                        pix.ChavePIX = "+55" + pix.ChavePIX;
            }

            return segmento;
        }

        internal override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoJ boleto)
            {
                if (boleto.CodigoInstrucaoMovimento == CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado)
                    boleto.CodigoInstrucaoMovimento = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado;
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

        #endregion
    }
}
 
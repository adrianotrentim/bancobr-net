using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Santander
{
    public class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 33, "Banco Santander", 60) 
        {     
        }

        #region ::. Instancias .::

        internal override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento) => new HeaderLote(this);

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

        internal override Febraban.TrailerArquivo NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #endregion

        internal override Febraban.HeaderArquivo PreencheHeaderArquivo(Febraban.HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            if (Empresa.Convenio.Substring(0, 4) != "0033")
            {
                headerArquivo.Convenio =
                    headerArquivo.CodigoBanco.ToString().PadLeft(4, '0') + //Nro Banco
                    Empresa.NumeroAgencia.ToString().PadLeft(4, '0') + //Cod Agência s/ dígito verificador
                    Empresa.Convenio.PadLeft(12, '0'); //N° Convênio
            }

            return headerArquivo;
        }

        internal override HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote, TipoLancamentoEnum tipoLancamento)
        {
            if (Empresa.Convenio.Substring(0, 4) != "0033")
            {
                ((Febraban.HeaderLote)headerLote).Convenio =
                ((Febraban.HeaderLote)headerLote).CodigoBanco.ToString().PadLeft(4, '0') + //Nro Banco
                Empresa.NumeroAgencia.ToString().PadLeft(4, '0') + //Cod Agência s/ dígito verificador
                Empresa.Convenio.PadLeft(12, '0'); //N° Convênio
            }

            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.PIXTransferencia:
                    ((Febraban.HeaderLote)headerLote).VersaoLote = 31; 
                    break;
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    ((Febraban.HeaderLote)headerLote).VersaoLote = 30; 
                    break;
                case TipoLancamentoEnum.PagamentoTributosCodigoBarra:
                    ((Febraban.HeaderLote)headerLote).VersaoLote = 10; 
                    break;
            }

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {
            switch (movimento.TipoLancamento)
            {
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                    ((SegmentoA_Transferencia)segmento).CodigoFinalidadeTED = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).CodigoFinalidadeTED;
                    ((Febraban.SegmentoA)segmento).CodigoFinalidadeComplementar = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta == TipoContaEnum.ContaCorrente ? "CC" : "PP";                    
                    break;
            }

            return segmento;
        }

        internal override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoB_Transferencia ted)
            {
                ted.CodigoHistoricoParaCredito = 183;
                ted.Vencimento = movimento.DataPagamento;
            }

            if (segmento is SegmentoB_PIX pix)
            {        
                if (pix.FormaIniciacao == FormaIniciacaoEnum.PIX_CPF_CNPJ) 
                {
                    pix.ChavePIX = pix.ChavePIX.JustNumbers();
                }
                else if (pix.FormaIniciacao == FormaIniciacaoEnum.PIX_Telefone) 
                {
                    if (!pix.ChavePIX.Contains("+55")) 
                    { 
                        pix.ChavePIX = "+55" + pix.ChavePIX.JustNumbers(); 
                    }
                }
            }

            return segmento;
        }        
    }
}

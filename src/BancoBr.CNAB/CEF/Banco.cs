using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.CEF
{
    public class Banco : Base.Banco
    {
        /***
        **** Convenio dve ser igual a:
        ****     Convenio = 6 posições
        ****     Tipo de Compromisso = 2 posicoes
        ****     Cod. do Compromisso = 4 posicos
        ****     Parametro de transmissao = 2 posicoes
        ***/

        public Banco(Correntista empresa)
            : base(empresa, 104, "CAIXA", 80)
        {
            if (Empresa.Convenio.Length > 14)
                throw new Exception("Convenio dve ser igual a: Convenio = 6 posições, Tipo de Compromisso = 2 posicoes, Cod. do Compromisso = 4 posicos, Parametro de transmissao = 2 posicoes");            
        }

        internal override Febraban.HeaderArquivo PreencheHeaderArquivo(Febraban.HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            headerArquivo.Convenio =
                Empresa.Convenio.PadLeft(14, '0').Substring(0, 6) +
                Empresa.Convenio.PadLeft(14, '0').Substring(12, 2) +
                "P" + //Ambiente Cliente
                " " + //Ambiente Caixa
                "   " + //Origem Aplicativo
                "0000" + //Nro Versão
                "   "; //Filler

            if (movimentos.Any(t => t.TipoLancamento == TipoLancamentoEnum.PIXTransferencia))
                throw new Exception("PIX não implementado.");
            //if (movimentos.Any(t => t.TipoLancamento == TipoLancamentoEnum.PIXTransferencia) && movimentos.Any(t => t.TipoLancamento != TipoLancamentoEnum.PIXTransferencia))
            //    throw new Exception("Para movimentos PIX CNAB204 CEF, os lançamentos devem ser somente PIX.");

            //if (movimentos.Any(t => t.TipoLancamento == TipoLancamentoEnum.PIXTransferencia))
            //    headerArquivo.ReservadoBanco = "PIX";

            return headerArquivo;
        }

        internal override HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote, TipoLancamentoEnum tipoLancamento)
        {
            ((HeaderLote)headerLote).Convenio = Empresa.Convenio.PadLeft(14, '0') + "      ";
            ((HeaderLote)headerLote).VersaoLote = 41;

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {

            ((Febraban.SegmentoA)segmento).NumeroDocumentoEmpresa = movimento.NumeroDocumento.PadLeft(6, '0') + 
                "".PadRight(13, ' ');

            switch (movimento.TipoLancamento)
            {
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                    ((Febraban.SegmentoA_Transferencia)segmento).NumeroDocumentoEmpresa += (((int)((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta) + 1).ToString();

                    ((Febraban.SegmentoA_Transferencia)segmento).CodigoFinalidadeTED = FinalidadeTEDEnum.NaoAplicavel;
                    ((Febraban.SegmentoA_Transferencia)segmento).CodigoFinalidadeComplementar = "";
                    break;
                default:
                    ((Febraban.SegmentoA)segmento).NumeroDocumentoEmpresa += "0";
                    break;
            }

            ((Febraban.SegmentoA)segmento).NumeroDocumentoBanco =
                "".PadLeft(9, ' ') + // Numero do Documento do Banco
                "".PadRight(3, ' ') + // Filler
                "01" + // Quantidade de Parcelas
                "N" + // Indica bloqueio de demais parcelas
                "0" + // Indicador da forma de parcelamento
                "  " + // Periodo de Vencimento do Parcelamento
                "00"; // Numero da Parcela

            return segmento;
        }

        internal override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento)
        {
            ((Febraban.SegmentoJ)segmento).NumeroDocumentoEmpresa = movimento.NumeroDocumento.PadLeft(6, '0') +
                "".PadRight(14, ' ');

            ((Febraban.SegmentoJ)segmento).NumeroDocumentoBanco =
                "".PadLeft(9, ' ') + // Numero do Documento do Banco
                "".PadRight(11, ' ');  // Filler

            return segmento;
        }
    }
}

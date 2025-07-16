using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Inter
{
    public class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 77, "BANCO INTER", 107) 
        {     
        }

        internal override Febraban.HeaderArquivo PreencheHeaderArquivo(Febraban.HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            headerArquivo.Convenio = "".PadRight(20, ' ');
            headerArquivo.DensidadeArquivo = 1600;

            return headerArquivo;
        }

        internal override HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote, TipoLancamentoEnum tipoLancamento)
        {
            ((HeaderLote)headerLote).Convenio = "".PadRight(20, ' ');
            //Versão Fixa do Inter para todas as movimentações
            //TED, PIX, Pagamentos de Cobranças com código de barras ou QR Code, Pagamentos de Convênios e Tributos com código de barras                   
            ((HeaderLote)headerLote).VersaoLote = 46; 

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoB_PIX pix)
            {
                if (pix.FormaIniciacao != FormaIniciacaoEnum.PIX_Telefone && pix.FormaIniciacao != FormaIniciacaoEnum.PIX_Email && pix.FormaIniciacao != FormaIniciacaoEnum.PIX_Aleatorio)
                    pix.ChavePIX = "".PadRight(99, ' ');
            }

            return segmento;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Bradesco
{
    public sealed class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 237, "BRADESCO", 89)
        {
        }

        internal override HeaderArquivo PreencheHeaderArquivo(HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            if (movimentos != null)
            {
                if (movimentos.Any(t => t.MovimentoItem is MovimentoItemTransferenciaPIX) && !movimentos.All(t => t.MovimentoItem is MovimentoItemTransferenciaPIX))
                    throw new Exception("Para o Bradesco, não é possível um único arquivo com registros PIX e outros tipos de registros. O arquivo PIX dese ser somente para PIX.");

                if (movimentos.Any(t => t.MovimentoItem is MovimentoItemTransferenciaPIX))
                    headerArquivo.ReservadoBanco = "PIX";
            }

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
                    ((HeaderLote)headerLote).VersaoLote = 45;
                    break;
            }

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoA_Transferencia transferencia)
            {
                transferencia.CodigoFinalidadeComplementar = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta == TipoContaEnum.ContaCorrente ? "CC" : "PP";
            }

            return segmento;
        }
    }
}

using System;
using System.Collections;
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
        public Banco()
            : base(237, "BRADESCO", 89)
        {
        }

        public override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.PIXTransferencia:
                    return new HeaderLote_Transferencia(this);
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    return new HeaderLote_PagamentoTitulo(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        public override RegistroBase NovoHeaderArquivo(Correntista correntista, int numeroRemessa, List<Movimento> movimentos)
        {
            var header = new HeaderArquivo(this, correntista, numeroRemessa);

            if (movimentos == null) 
                return header;

            if (movimentos.Any(t => t.MovimentoItem is MovimentoItemTransferenciaPIX) && !movimentos.All(t => t.MovimentoItem is MovimentoItemTransferenciaPIX))
                throw new Exception("Para o Bradesco, não é possível um único arquivo com registros PIX e outros tipos de registros. O arquivo PIX dese ser somente para PIX.");

            if (movimentos.Any(t => t.MovimentoItem is MovimentoItemTransferenciaPIX))
                header.ReservadoBanco = "PIX";

            return header;
        }

        public override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Common.Instances.Movimento movimento)
        {
            if (segmento is SegmentoA_Transferencia transferencia)
            {
                transferencia.CodigoFinalidadeComplementar = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta == TipoContaEnum.ContaCorrente ? "CC" : "PP";
            }

            return segmento;
        }

        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
    }
}

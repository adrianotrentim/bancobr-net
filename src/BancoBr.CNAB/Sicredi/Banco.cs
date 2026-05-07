using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Sicredi
{
    public class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 748, "SICREDI", 82) 
        {     
        }

        internal override Febraban.HeaderArquivo PreencheHeaderArquivo(Febraban.HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            headerArquivo.Convenio =
                Empresa.Convenio.PadLeft(4, '0') +
                "".PadRight(16, ' '); //Filler

            return headerArquivo;
        }

        internal override HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote, TipoLancamentoEnum tipoLancamento)
        {
            ((HeaderLote)headerLote).VersaoLote = 42;
            ((HeaderLote)headerLote).Convenio = Empresa.Convenio.PadLeft(4, '0') +
                "".PadRight(16, ' '); //Filler

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {
            if(movimento.CodigoInstrucao == CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado) // Se o movimento for bloqueado, deve ser liberado porque a Sicredi não tem a opção 09
                ((Febraban.SegmentoA)segmento).CodigoInstrucaoMovimento = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado; //Cód. Instr. Movimento

            return segmento;
        }

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

        internal override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (movimento.CodigoInstrucao == CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado) // Se o movimento for bloqueado, deve ser liberado porque a Sicredi não tem a opção 09
                ((Febraban.SegmentoJ)segmento).CodigoInstrucaoMovimento = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado; //Cód. Instr. Movimento

            return segmento;
        }

        internal override HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento)
        {   
            if (this.ServicoAtual == TipoServicoEnum.PagamentoSalarios)
                return new HeaderLote(this);

            return base.NovoHeaderLote(tipoLancamento);
        }

        internal override RegistroDetalheBase NovoSegmentoA(TipoLancamentoEnum tipoLancamento)
        {
            if (this.ServicoAtual == TipoServicoEnum.PagamentoSalarios)
                return new SegmentoA_TransferenciaFolha(this);

            return base.NovoSegmentoA(tipoLancamento);
        }


    }
}

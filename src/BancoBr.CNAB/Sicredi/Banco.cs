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
            : base(empresa, 748, "SICREDI", 80) 
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

        internal override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (movimento.CodigoInstrucao == CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado) // Se o movimento for bloqueado, deve ser liberado porque a Sicredi não tem a opção 09
                ((Febraban.SegmentoJ)segmento).CodigoInstrucaoMovimento = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado; //Cód. Instr. Movimento

            return segmento;
        }
    }
}

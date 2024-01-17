using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Enums;

namespace BancoBr.Common.Instances
{
    public class Movimento
    {
        public Movimento()
        {
            TipoMovimento = TipoMovimentoEnum.Inclusao;
            CodigoInstrucao = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado;
            Moeda = "BRL";
            QuantidadeMoeda = 0;
        }

        public Pessoa PessoaEmpresaDestino { get; set; }
        public TipoMovimentoEnum TipoMovimento { get; set; }
        public TipoLancamentoEnum TipoLancamento { get; set; }

        public CodigoInstrucaoMovimentoEnum CodigoInstrucao { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Moeda { get; set; }
        public int QuantidadeMoeda { get; set; }
        public decimal ValorPagamento { get; set; }

        /// <summary>
        /// Finalidade da Operação para DOC ou TED - Verificar nota P005 e P001 do CNAB240
        /// </summary>
        public FinalidadeTEDEnum CodigoFinalidadeTED { get; set; }

        public AvisoFavorecidoEnum AvisoAoFavorecido { get; set; }
    }
}

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
        }

        public Pessoa PessoaEmpresaDestino { get; set; }
        public TipoMovimentoEnum TipoMovimento { get; set; }
        public TipoLancamentoEnum TipoLancamento { get; set; }
        public CodigoInstrucaoMovimentoEnum CodigoInstrucao { get; set; }

        
        public MovimentoPagamentoTransferencia MovimentoPagamentoTransferencia { get; set; }
        public MovimentoTituloCodigoBarra MovimentoTituloCodigoBarra { get; set; }
        

        public AvisoFavorecidoEnum AvisoAoFavorecido { get; set; }
    }

    public class MovimentoPagamentoTransferencia
    {
        public MovimentoPagamentoTransferencia()
        {
            Moeda = "BRL";
            QuantidadeMoeda = 0;
        }

        public string NumeroDocumento { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Moeda { get; set; }
        public int QuantidadeMoeda { get; set; }
        public decimal ValorPagamento { get; set; }

        /// <summary>
        /// Finalidade da Operação para TED - Verificar nota P011 do CNAB240
        /// </summary>
        public FinalidadeTEDEnum CodigoFinalidadeTED { get; set; }

        public FormaIniciacaoEnum TipoChavePIX { get; set; }
        public string ChavePIX { get; set; }
    }

    public class MovimentoTituloCodigoBarra
    {
        public MovimentoTituloCodigoBarra()
        {
            MoedaCodigoBarra = 9;
        }

        /// <summary>
        /// Código do banco do código de barras
        /// </summary>
        public int BancoCodigoBarra { get; set; }

        /// <summary>
        /// Moeda no código de barras
        /// </summary>
        public int MoedaCodigoBarra { get; set; }

        /// <summary>
        /// DV do código de barras
        /// </summary>
        public int DVCodigoBarra { get; set; }

        /// <summary>
        /// Fator de Vencimento do código de barras
        /// </summary>
        public int FatorVencimentoCodigoBarra { get; set; }

        /// <summary>
        /// Valor do código de barras
        /// </summary>
        public int ValorCodigoBarra { get; set; }

        /// <summary>
        /// Campo Livre do código de barras - 25 posições
        /// </summary>
        public string CampoLivreCodigoBarra { get; set; }

        public decimal ValorTitulo { get; set; }
        public decimal Desconto { get; set; }
        public decimal Acrescimo { get; set; }
        public DateTime DataPagamento { get; set; }
        public decimal ValorPagamento { get; set; }
        public int QuantidadeMoeda { get; set; }
        public string NumeroDocumento { get; set; }
    }
}

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

        public Favorecido Favorecido { get; set; }
        public TipoMovimentoEnum TipoMovimento { get; set; }
        public TipoLancamentoEnum TipoLancamento { get; set; }
        public CodigoInstrucaoMovimentoEnum CodigoInstrucao { get; set; }

        public string Moeda { get; set; }
        public decimal QuantidadeMoeda { get; set; }
        public DateTime DataPagamento { get; set; }
        public string NumeroDocumento { get; set; }
        public decimal ValorPagamento { get; set; }

        public string NumeroDocumentoNoBanco { get; set; }

        public MovimentoItem MovimentoItem { get; set; }
        public AvisoFavorecidoEnum AvisoAoFavorecido { get; set; }
        public List<Ocorrencia> Ocorrencias { get; set; }
        public string Registro { get; set; }
    }

    public class MovimentoItem
    {
    }

    public class MovimentoItemTransferenciaPIX : MovimentoItem
    {
        public FormaIniciacaoEnum TipoChavePIX { get; set; }
        public string ChavePIX { get; set; }
    }

    public class MovimentoItemTransferenciaTED : MovimentoItem
    {
        public int Banco { get; set; }
        public int NumeroAgencia { get; set; }
        public string DVAgencia { get; set; }
        public int NumeroConta { get; set; }
        public string DVConta { get; set; }
        public FinalidadeTEDEnum CodigoFinalidadeTED { get; set; }
        public TipoContaEnum TipoConta { get; set; }
    }

    public class MovimentoItemPagamentoTituloCodigoBarra : MovimentoItem
    {
        public MovimentoItemPagamentoTituloCodigoBarra()
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
        public decimal ValorCodigoBarra { get; set; }

        /// <summary>
        /// Campo Livre do código de barras - 25 posições
        /// </summary>
        public string CampoLivreCodigoBarra { get; set; }

        public decimal Desconto { get; set; }
        public decimal Acrescimo { get; set; }
    }
}

using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Resultado retornado pelo pagamento (200), pela consulta de comprovante por id e pela
    /// consulta de comprovante por idempotency key. O Sicoob envelopa em "resultado",
    /// resolvido internamente pelo PagamentoBoletoClient.
    /// </summary>
    internal class ComprovantePagamento
    {
        [JsonProperty("numeroAgencia")]
        public string NumeroAgencia { get; set; }

        [JsonProperty("nomeAgencia")]
        public string NomeAgencia { get; set; }

        [JsonProperty("numeroConta")]
        public long NumeroConta { get; set; }

        [JsonProperty("nomeProprietarioContaCorrente")]
        public string NomeProprietarioContaCorrente { get; set; }

        [JsonProperty("numeroLinhaDigitavel")]
        public string NumeroLinhaDigitavel { get; set; }

        [JsonProperty("numeroInstituicaoEmissora")]
        public int NumeroInstituicaoEmissora { get; set; }

        [JsonProperty("nomeInstituicaoEmissora")]
        public string NomeInstituicaoEmissora { get; set; }

        [JsonProperty("numeroCpfCnpjBeneficiario")]
        public string NumeroCpfCnpjBeneficiario { get; set; }

        [JsonProperty("nomeRazaoSocialBeneficiario")]
        public string NomeRazaoSocialBeneficiario { get; set; }

        [JsonProperty("numeroCpfCnpjPagador")]
        public string NumeroCpfCnpjPagador { get; set; }

        [JsonProperty("nomeRazaoSocialPagador")]
        public string NomeRazaoSocialPagador { get; set; }

        [JsonProperty("dataVencimento")]
        public DateTime DataVencimento { get; set; }

        [JsonProperty("valorBoleto")]
        public decimal ValorBoleto { get; set; }

        [JsonProperty("valorAbatimentoDesconto")]
        public decimal ValorAbatimentoDesconto { get; set; }

        [JsonProperty("valorMultaMora")]
        public decimal ValorMultaMora { get; set; }

        [JsonProperty("valorPagamento")]
        public decimal ValorPagamento { get; set; }

        [JsonProperty("dataPagamento")]
        public DateTime DataPagamento { get; set; }

        [JsonProperty("situacaoPagamento")]
        public string SituacaoPagamento { get; set; }

        [JsonProperty("descricaoDetalheSituacao")]
        public string DescricaoDetalheSituacao { get; set; }

        [JsonProperty("dataHoraCadastro")]
        public DateTime? DataHoraCadastro { get; set; }

        [JsonProperty("aceitaValorDivergente")]
        public bool AceitaValorDivergente { get; set; }

        [JsonProperty("nossoNumero")]
        public string NossoNumero { get; set; }

        [JsonProperty("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        [JsonProperty("descricaoObservacao")]
        public string DescricaoObservacao { get; set; }

        [JsonProperty("descricaoOuvidoria")]
        public string DescricaoOuvidoria { get; set; }

        [JsonProperty("descricaoTituloComprovante")]
        public string DescricaoTituloComprovante { get; set; }

        [JsonProperty("idPagamento")]
        public long IdPagamento { get; set; }

        [JsonProperty("numeroAutenticacaoPagamento")]
        public string NumeroAutenticacaoPagamento { get; set; }
    }
}

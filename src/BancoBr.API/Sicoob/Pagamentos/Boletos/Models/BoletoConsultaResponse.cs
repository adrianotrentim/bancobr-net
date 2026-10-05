using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Resultado de GET /boletos/{codigoBarras}. O Sicoob envelopa a resposta em "resultado";
    /// o envelope é resolvido internamente pelo PagamentoBoletoClient.
    /// </summary>
    internal class BoletoConsultaResponse
    {
        [JsonProperty("numeroInstituicaoEmissora")]
        public int NumeroInstituicaoEmissora { get; set; }

        [JsonProperty("nomeInstituicaoEmissora")]
        public string NomeInstituicaoEmissora { get; set; }

        [JsonProperty("tipoPessoaBeneficiario")]
        public string TipoPessoaBeneficiario { get; set; }

        [JsonProperty("numeroCpfCnpjBeneficiario")]
        public string NumeroCpfCnpjBeneficiario { get; set; }

        [JsonProperty("nomeRazaoSocialBeneficiario")]
        public string NomeRazaoSocialBeneficiario { get; set; }

        [JsonProperty("nomeFantasiaBeneficiario")]
        public string NomeFantasiaBeneficiario { get; set; }

        [JsonProperty("tipoPessoaPagador")]
        public string TipoPessoaPagador { get; set; }

        [JsonProperty("numeroCpfCnpjPagador")]
        public string NumeroCpfCnpjPagador { get; set; }

        [JsonProperty("nomeRazaoSocialPagador")]
        public string NomeRazaoSocialPagador { get; set; }

        [JsonProperty("codigoBarras")]
        public string CodigoBarras { get; set; }

        [JsonProperty("numeroLinhaDigitavel")]
        public string NumeroLinhaDigitavel { get; set; }

        [JsonProperty("dataVencimentoBoleto")]
        public DateTime DataVencimentoBoleto { get; set; }

        [JsonProperty("dataLimitePagamentoBoleto")]
        public DateTime? DataLimitePagamentoBoleto { get; set; }

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

        [JsonProperty("permiteAlterarValor")]
        public bool PermiteAlterarValor { get; set; }

        [JsonProperty("consultaEmContingencia")]
        public bool ConsultaEmContingencia { get; set; }

        [JsonProperty("codigoEspecieDocumento")]
        public int? CodigoEspecieDocumento { get; set; }

        [JsonProperty("codigoSituacaoBoletoPagamento")]
        public string CodigoSituacaoBoletoPagamento { get; set; }

        [JsonProperty("nossoNumero")]
        public string NossoNumero { get; set; }

        [JsonProperty("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        /// <summary>
        /// Identificador retornado pela consulta. Deve ser enviado de volta no corpo de
        /// POST /boletos/pagamentos/{codigoBarras} (BoletoPagamentoRequest.IdentificadorConsulta).
        /// </summary>
        [JsonProperty("identificadorConsulta")]
        public string IdentificadorConsulta { get; set; }

        [JsonProperty("descricaoInstrucaoValorMinMax")]
        public string DescricaoInstrucaoValorMinMax { get; set; }

        [JsonProperty("bloquearPagamento")]
        public bool BloquearPagamento { get; set; }

        [JsonProperty("mensagemBloqueioPagamento")]
        public string MensagemBloqueioPagamento { get; set; }
    }
}

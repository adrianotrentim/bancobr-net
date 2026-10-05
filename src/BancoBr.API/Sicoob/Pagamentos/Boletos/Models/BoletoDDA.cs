using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Item retornado por GET /boletos (consulta de boletos DDA de uma conta corrente).
    /// </summary>
    internal class BoletoDDA
    {
        [JsonProperty("descricaoTipoPagador")]
        public string DescricaoTipoPagador { get; set; }

        [JsonProperty("tipoPessoaBeneficiario")]
        public string TipoPessoaBeneficiario { get; set; }

        [JsonProperty("numeroCpfCnpjBeneficiario")]
        public string NumeroCpfCnpjBeneficiario { get; set; }

        [JsonProperty("nomeRazaoSocialBeneficiario")]
        public string NomeRazaoSocialBeneficiario { get; set; }

        [JsonProperty("tipoPessoaPagador")]
        public string TipoPessoaPagador { get; set; }

        [JsonProperty("numeroCpfCnpjPagador")]
        public string NumeroCpfCnpjPagador { get; set; }

        [JsonProperty("nomeRazaoSocialPagador")]
        public string NomeRazaoSocialPagador { get; set; }

        [JsonProperty("nomeFantasiaPagador")]
        public string NomeFantasiaPagador { get; set; }

        [JsonProperty("descricaoLogradouroPagador")]
        public string DescricaoLogradouroPagador { get; set; }

        [JsonProperty("descricaoCidadePagador")]
        public string DescricaoCidadePagador { get; set; }

        [JsonProperty("siglaUfPagador")]
        public string SiglaUfPagador { get; set; }

        [JsonProperty("numeroCepPagador")]
        public string NumeroCepPagador { get; set; }

        [JsonProperty("tipoPessoaAvalista")]
        public string TipoPessoaAvalista { get; set; }

        [JsonProperty("numeroCpfCnpjAvalista")]
        public string NumeroCpfCnpjAvalista { get; set; }

        [JsonProperty("nomeAvalista")]
        public string NomeAvalista { get; set; }

        [JsonProperty("valorBoleto")]
        public decimal ValorBoleto { get; set; }

        [JsonProperty("dataVencimentoBoleto")]
        public DateTime DataVencimentoBoleto { get; set; }

        [JsonProperty("codigoTipoSituacaoBoleto")]
        public int CodigoTipoSituacaoBoleto { get; set; }

        [JsonProperty("descricaoSituacaoBoleto")]
        public string DescricaoSituacaoBoleto { get; set; }

        [JsonProperty("numeroIdentificadorBoletoCip")]
        public long NumeroIdentificadorBoletoCip { get; set; }

        [JsonProperty("numeroCodigoBarras")]
        public string NumeroCodigoBarras { get; set; }

        [JsonProperty("numeroCpfCnpjPagadorEletronico")]
        public string NumeroCpfCnpjPagadorEletronico { get; set; }

        [JsonProperty("aceite")]
        public bool Aceite { get; set; }

        [JsonProperty("numeroNossoNumero")]
        public string NumeroNossoNumero { get; set; }

        [JsonProperty("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        [JsonProperty("dataPagamento")]
        public DateTime? DataPagamento { get; set; }

        [JsonProperty("valorPagamento")]
        public decimal? ValorPagamento { get; set; }

        [JsonProperty("codigoEspecieDocumento")]
        public int CodigoEspecieDocumento { get; set; }

        [JsonProperty("dataEmissao")]
        public DateTime DataEmissao { get; set; }

        [JsonProperty("dataLimitePagamento")]
        public string DataLimitePagamento { get; set; }

        [JsonProperty("codigoTipoJuros")]
        public int CodigoTipoJuros { get; set; }

        [JsonProperty("dataJuros")]
        public DateTime? DataJuros { get; set; }

        [JsonProperty("valorPercentualJuros")]
        public decimal ValorPercentualJuros { get; set; }

        [JsonProperty("codigoTipoMulta")]
        public int CodigoTipoMulta { get; set; }

        [JsonProperty("dataMulta")]
        public DateTime? DataMulta { get; set; }

        [JsonProperty("valorPercentualMulta")]
        public decimal ValorPercentualMulta { get; set; }

        [JsonProperty("valorAbatimento")]
        public decimal ValorAbatimento { get; set; }

        [JsonProperty("codigoTipoDesconto1")]
        public string CodigoTipoDesconto1 { get; set; }

        [JsonProperty("dataDesconto1")]
        public DateTime? DataDesconto1 { get; set; }

        [JsonProperty("valorPercentualDesconto1")]
        public decimal ValorPercentualDesconto1 { get; set; }

        [JsonProperty("codigoTipoDesconto2")]
        public string CodigoTipoDesconto2 { get; set; }

        [JsonProperty("dataDesconto2")]
        public string DataDesconto2 { get; set; }

        [JsonProperty("valorPercentualDesconto2")]
        public decimal ValorPercentualDesconto2 { get; set; }

        [JsonProperty("codigoTipoDesconto3")]
        public string CodigoTipoDesconto3 { get; set; }

        [JsonProperty("dataDesconto3")]
        public string DataDesconto3 { get; set; }

        [JsonProperty("valorPercentualDesconto3")]
        public decimal ValorPercentualDesconto3 { get; set; }

        [JsonProperty("numeroDiasProtesto")]
        public int NumeroDiasProtesto { get; set; }

        [JsonProperty("quantidadePagamentoParcial")]
        public int QuantidadePagamentoParcial { get; set; }

        [JsonProperty("codigoAutorizacaoValorDivergente")]
        public int CodigoAutorizacaoValorDivergente { get; set; }

        [JsonProperty("codigoIndicadorValorMaximo")]
        public string CodigoIndicadorValorMaximo { get; set; }

        [JsonProperty("valorPercentualMaximo")]
        public decimal ValorPercentualMaximo { get; set; }

        [JsonProperty("codigoIndicadorValorMinimo")]
        public string CodigoIndicadorValorMinimo { get; set; }

        [JsonProperty("valorPercentualMinimo")]
        public decimal ValorPercentualMinimo { get; set; }
    }
}

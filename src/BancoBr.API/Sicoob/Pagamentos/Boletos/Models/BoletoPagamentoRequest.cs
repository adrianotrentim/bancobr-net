using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Corpo de POST /boletos/pagamentos/{codigoBarras}.
    /// </summary>
    internal class BoletoPagamentoRequest
    {
        /// <summary>Identificador retornado pela consulta prévia (BoletoConsultaResponse.IdentificadorConsulta).</summary>
        [JsonProperty("identificadorConsulta")]
        public string IdentificadorConsulta { get; set; }

        [JsonProperty("valorBoleto")]
        public decimal ValorBoleto { get; set; }

        [JsonProperty("valorDescontoAbatimento")]
        public decimal ValorDescontoAbatimento { get; set; }

        [JsonProperty("valorMultaMora")]
        public decimal ValorMultaMora { get; set; }

        [JsonProperty("descricaoObservacao")]
        public string DescricaoObservacao { get; set; }

        [JsonProperty("aceitaValorDivergente")]
        public bool AceitaValorDivergente { get; set; }

        [JsonProperty("numeroCpfCnpjPortador")]
        public string NumeroCpfCnpjPortador { get; set; }

        [JsonProperty("nomePortador")]
        public string NomePortador { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("date")]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; }

        [JsonProperty("debtorAccount")]
        public DebtorAccount DebtorAccount { get; set; }
    }
}

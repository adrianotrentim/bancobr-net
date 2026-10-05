using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Dados da arrecadação retornados dentro de ArrecadacaoResultado.Arrecadacao
    /// (resposta de POST /arrecadacao/codigo-barras/{codigoBarras}/pagamentos).
    /// </summary>
    internal class Arrecadacao
    {
        [JsonProperty("valorPago")]
        public decimal ValorPago { get; set; }

        [JsonProperty("nsu")]
        public long? Nsu { get; set; }

        [JsonProperty("dataPagamento")]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime DataPagamento { get; set; }

        [JsonProperty("valorDocumento")]
        public decimal ValorDocumento { get; set; }

        [JsonProperty("valorDesconto")]
        public decimal ValorDesconto { get; set; }

        [JsonProperty("valorJuros")]
        public decimal ValorJuros { get; set; }

        [JsonProperty("valorMulta")]
        public decimal ValorMulta { get; set; }

        [JsonProperty("autenticacao")]
        public string Autenticacao { get; set; }

        [JsonProperty("recebimentoViaCaixa")]
        public bool? RecebimentoViaCaixa { get; set; }
    }
}

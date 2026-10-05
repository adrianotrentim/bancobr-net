using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Item retornado por GET /arrecadacao/codigo-barras/{codigoBarras}/pagamentos: consulta de
    /// pagamento(s) já realizado(s) para um código de barras numa data de movimento.
    /// </summary>
    internal class ArrecadacaoConsultaItem
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

        [JsonProperty("identificadorFgts")]
        public string IdentificadorFgts { get; set; }

        [JsonProperty("anoExercicio")]
        public int? AnoExercicio { get; set; }

        [JsonProperty("recebimentoViaCaixa")]
        public bool? RecebimentoViaCaixa { get; set; }

        [JsonProperty("autenticacao")]
        public string Autenticacao { get; set; }

        [JsonProperty("situacao")]
        public SituacaoArrecadacao Situacao { get; set; }

        [JsonProperty("convenio")]
        public string Convenio { get; set; }

        [JsonProperty("siglaConvenio")]
        public string SiglaConvenio { get; set; }

        [JsonProperty("transacao")]
        public long? Transacao { get; set; }
    }
}

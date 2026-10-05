using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Resultado de GET /arrecadacao/codigo-barras/{codigoBarras}: informações do código de
    /// barras antes do pagamento.
    /// </summary>
    internal class ConvenioConsultaResponse
    {
        [JsonProperty("convenio")]
        public string Convenio { get; set; }

        [JsonProperty("siglaConvenio")]
        public string SiglaConvenio { get; set; }

        [JsonProperty("valorDocumento")]
        public decimal ValorDocumento { get; set; }

        [JsonProperty("valorDesconto")]
        public decimal ValorDesconto { get; set; }

        [JsonProperty("valorMulta")]
        public decimal ValorMulta { get; set; }

        [JsonProperty("valorJuros")]
        public decimal ValorJuros { get; set; }

        [JsonProperty("valorOutrosEncargos")]
        public decimal ValorOutrosEncargos { get; set; }

        [JsonProperty("valorTotal")]
        public decimal ValorTotal { get; set; }

        [JsonProperty("codigoConvenioFebraban")]
        public string CodigoConvenioFebraban { get; set; }

        [JsonProperty("nsu")]
        public long? Nsu { get; set; }

        [JsonProperty("transacao")]
        public long Transacao { get; set; }
    }
}

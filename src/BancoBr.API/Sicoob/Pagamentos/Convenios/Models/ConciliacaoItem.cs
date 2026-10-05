using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Item retornado por GET /arrecadacao/conciliacoes: informações sintéticas das
    /// arrecadações realizadas numa data de movimento.
    /// </summary>
    internal class ConciliacaoItem
    {
        [JsonProperty("situacao")]
        public string Situacao { get; set; }

        [JsonProperty("convenio")]
        public string Convenio { get; set; }

        [JsonProperty("siglaConvenio")]
        public string SiglaConvenio { get; set; }

        [JsonProperty("valorTotal")]
        public decimal ValorTotal { get; set; }

        [JsonProperty("quantidade")]
        public int Quantidade { get; set; }
    }
}

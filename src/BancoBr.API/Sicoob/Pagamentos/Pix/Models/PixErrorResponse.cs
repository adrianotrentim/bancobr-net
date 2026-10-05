using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Corpo de erro (application/problem+json) retornado pela API Pix Pagamentos do
    /// Sicoob — formato diferente do { "mensagens": [...] } usado pelas APIs de
    /// Boletos/Convênios.
    /// </summary>
    internal class PixErrorResponse
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("violacoes")]
        public List<PixViolacao> Violacoes { get; set; }
    }
}

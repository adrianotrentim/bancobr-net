using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    internal class PixViolacao
    {
        [JsonProperty("razao")]
        public string Razao { get; set; }

        [JsonProperty("propriedade")]
        public string Propriedade { get; set; }
    }
}

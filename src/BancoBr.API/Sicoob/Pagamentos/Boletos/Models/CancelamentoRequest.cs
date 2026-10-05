using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    internal class CancelamentoRequest
    {
        [JsonProperty("numeroConta")]
        public long NumeroConta { get; set; }
    }
}

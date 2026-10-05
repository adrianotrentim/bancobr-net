using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>
    /// Item de erro da API SPB Transferências — diferente do formato { "mensagens": [...] }
    /// usado por Boletos/Convênios, aqui a resposta de erro é um array bruto de objetos
    /// { code, title, detail }.
    /// </summary>
    internal class TedErrorItem
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }
    }
}

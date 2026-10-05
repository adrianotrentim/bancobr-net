using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.ContaCorrente.Models
{
    /// <summary>
    /// Item de erro da API Conta Corrente — diferente do formato { "mensagens": [...] }
    /// usado por Boletos/Convênios, aqui a resposta de erro vem como
    /// { "errors": [{ "code", "title", "detail" }], "meta": { ... } }.
    /// </summary>
    internal class ContaCorrenteErrorItem
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }
    }
}
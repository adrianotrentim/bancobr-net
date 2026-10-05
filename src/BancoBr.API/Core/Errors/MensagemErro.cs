using Newtonsoft.Json;

namespace BancoBr.API.Core.Errors
{
    /// <summary>Uma mensagem de erro de negócio, normalizada a partir do formato próprio de cada banco.</summary>
    public class MensagemErro
    {
        [JsonProperty("mensagem")]
        public string Mensagem { get; set; }

        [JsonProperty("codigo")]
        public string Codigo { get; set; }
    }
}

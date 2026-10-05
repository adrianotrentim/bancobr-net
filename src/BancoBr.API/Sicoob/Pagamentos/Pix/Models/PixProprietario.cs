using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Dados do titular da chave Pix resolvida pela iniciação (POST /pagamentos).
    /// </summary>
    internal class PixProprietario
    {
        [JsonProperty("identificador")]
        public string Identificador { get; set; }

        [JsonProperty("nome")]
        public string Nome { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; }
    }
}

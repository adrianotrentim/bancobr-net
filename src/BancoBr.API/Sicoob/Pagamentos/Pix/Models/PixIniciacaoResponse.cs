using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Retorno da iniciação de pagamento por chave DICT (POST /pagamentos) — resolve a
    /// chave para os dados do titular, sem movimentar valores.
    /// </summary>
    internal class PixIniciacaoResponse
    {
        [JsonProperty("endToEndId")]
        public string EndToEndId { get; set; }

        [JsonProperty("chave")]
        public string Chave { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; }

        [JsonProperty("proprietario")]
        public PixProprietario Proprietario { get; set; }
    }
}

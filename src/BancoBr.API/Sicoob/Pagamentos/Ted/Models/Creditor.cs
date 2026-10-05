using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>Beneficiário da TED.</summary>
    internal class Creditor
    {
        /// <summary>NATURAL_PERSON (pessoa física) ou LEGAL_PERSON (pessoa jurídica).</summary>
        [JsonProperty("personType")]
        public string PersonType { get; set; }

        [JsonProperty("cpfCnpj")]
        public string CpfCnpj { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}

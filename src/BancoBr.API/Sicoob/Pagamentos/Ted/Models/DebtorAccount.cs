using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>
    /// Conta do associado utilizada para débito da TED. Não confundir com
    /// <see cref="Boletos.Models.DebtorAccount"/> — a API SPB Transferências representa
    /// accountType/personType como texto (CACC/NATURAL_PERSON, ...), não como código
    /// numérico como a API de Boletos.
    /// </summary>
    internal class DebtorAccount
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        /// <summary>CACC (Conta-Corrente), SLRY (Conta-Salário), SVGS (Conta-Poupança) ou TRAN (Conta-Pagamento).</summary>
        [JsonProperty("accountType")]
        public string AccountType { get; set; }

        /// <summary>NATURAL_PERSON (pessoa física) ou LEGAL_PERSON (pessoa jurídica).</summary>
        [JsonProperty("personType")]
        public string PersonType { get; set; }

        [JsonProperty("ispb")]
        public string Ispb { get; set; }
    }
}

using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>Conta do beneficiário da TED.</summary>
    internal class CreditorAccount
    {
        [JsonProperty("ispb")]
        public string Ispb { get; set; }

        /// <summary>
        /// String, não number, apesar do que a doc do Sicoob sugere — a API valida
        /// (ERRO_TAMANHO_NUMEROAGENCIA) que a agência venha com 4 dígitos, zero à esquerda.
        /// </summary>
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
    }
}

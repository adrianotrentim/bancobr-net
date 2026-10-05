using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Conta do associado utilizada para débito do pagamento.
    /// </summary>
    internal class DebtorAccount
    {
        /// <summary>Número da cooperativa da conta.</summary>
        [JsonProperty("issuer")]
        public int Issuer { get; set; }

        /// <summary>Número da conta habilitada para pagamentos via API.</summary>
        [JsonProperty("number")]
        public long Number { get; set; }

        /// <summary>0 - Conta Corrente.</summary>
        [JsonProperty("accountType")]
        public int AccountType { get; set; }

        /// <summary>0 - Pessoa Física, 1 - Pessoa Jurídica.</summary>
        [JsonProperty("personType")]
        public int PersonType { get; set; }
    }
}

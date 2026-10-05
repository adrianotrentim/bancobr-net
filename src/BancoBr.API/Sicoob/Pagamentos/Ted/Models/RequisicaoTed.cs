using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>Corpo de POST /transferencias.</summary>
    internal class RequisicaoTed
    {
        [JsonProperty("debtorAccount")]
        public DebtorAccount DebtorAccount { get; set; }

        [JsonProperty("creditorAccount")]
        public CreditorAccount CreditorAccount { get; set; }

        [JsonProperty("creditor")]
        public Creditor Creditor { get; set; }

        /// <summary>Data de movimento da TED, formato "yyyy-MM-dd".</summary>
        [JsonProperty("date")]
        public string Date { get; set; }

        /// <summary>Valor da TED, formato "0.00" (ponto decimal, conforme exemplo do Sicoob).</summary>
        [JsonProperty("amount")]
        public string Amount { get; set; }

        /// <summary>Código de finalidade da TED (SPB/Febraban), 5 dígitos.</summary>
        [JsonProperty("finalidade")]
        public string Finalidade { get; set; }

        [JsonProperty("numeroPa")]
        public string NumeroPa { get; set; }

        [JsonProperty("historico")]
        public string Historico { get; set; }
    }
}

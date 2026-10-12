using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.ContaCorrente.Models
{
    /// <summary>Página do GET /banks/{bank_id}/statements/{statement_id} (bank_account_information v1).</summary>
    internal class ExtratoResponse
    {
        [JsonProperty("_content")]
        public List<ExtratoTransacaoResponse> Content { get; set; }
    }

    internal class ExtratoTransacaoResponse
    {
        /// <summary>Tipo do lançamento: crédito ou débito.</summary>
        [JsonProperty("creditDebitType")]
        public string CreditDebitType { get; set; }

        [JsonProperty("transactionName")]
        public string TransactionName { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("transactionDate")]
        public DateTime TransactionDate { get; set; }
    }
}

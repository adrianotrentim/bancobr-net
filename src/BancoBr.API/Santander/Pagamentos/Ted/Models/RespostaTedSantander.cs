using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Ted.Models
{
    /// <summary>
    /// Resposta do POST (201), do PATCH (200) e do GET de uma TED — o Santander devolve o mesmo
    /// objeto nos três. O <c>transaction</c> só aparece na consulta.
    /// </summary>
    internal class RespostaTedSantander
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Textual: READY_TO_PAY, PENDING_CONFIRMATION, PAYED, REJECTED ou REFUNDED.</summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("rejectReason")]
        public string RejectReason { get; set; }

        [JsonProperty("paymentDate")]
        public string PaymentDate { get; set; }

        [JsonProperty("destinationAccount")]
        public TedContaDestino DestinationAccount { get; set; }

        [JsonProperty("identifierTransferCode")]
        public string IdentifierTransferCode { get; set; }

        [JsonProperty("transferHistory")]
        public string TransferHistory { get; set; }

        [JsonProperty("transferValue")]
        public decimal TransferValue { get; set; }

        [JsonProperty("transaction")]
        public TedTransacao Transaction { get; set; }
    }

    internal class TedTransacao
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>Código de autenticação da transferência.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }
    }

    /// <summary>Corpo da efetivação da TED (PATCH .../transfer/{id}).</summary>
    internal class RequisicaoConfirmacaoTed
    {
        [JsonProperty("sourceAccount")]
        public TedContaOrigem SourceAccount { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "AUTHORIZED";
    }
}

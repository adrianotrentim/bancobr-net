using System;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Models
{
    /// <summary>
    /// Resposta do POST (201), do PATCH (200) e do GET de um pagamento de boleto
    /// (bank_slip_payments) ou de código de barras (barcode_payments) — o mesmo objeto nos
    /// três. Os campos específicos de cada modalidade vêm nulos na outra: vencimento, valores
    /// e partes do boleto; tipo de pagamento e concessionária do código de barras.
    /// </summary>
    internal class PagamentoCodigoResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>Textual: PENDING_VALIDATION, READY_TO_PAY, PENDING_CONFIRMATION, PAYED, REFUNDED ou REJECTED.</summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("rejectReason")]
        public string RejectReason { get; set; }

        [JsonProperty("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("nominalValue")]
        public decimal? NominalValue { get; set; }

        [JsonProperty("deductedValue")]
        public decimal? DeductedValue { get; set; }

        [JsonProperty("addedValue")]
        public decimal? AddedValue { get; set; }

        [JsonProperty("totalValue")]
        public decimal? TotalValue { get; set; }

        [JsonProperty("paymentValue")]
        public decimal? PaymentValue { get; set; }

        /// <summary>Código de barras: UTILITY (contas de consumo) ou TAXES (tributos).</summary>
        [JsonProperty("paymentType")]
        public string PaymentType { get; set; }

        [JsonProperty("payer")]
        public PixBeneficiario Payer { get; set; }

        [JsonProperty("beneficiary")]
        public PixBeneficiario Beneficiary { get; set; }

        [JsonProperty("concessionary")]
        public ConcessionariaPagamento Concessionary { get; set; }

        [JsonProperty("transaction")]
        public PixTransacao Transaction { get; set; }
    }

    internal class ConcessionariaPagamento
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}

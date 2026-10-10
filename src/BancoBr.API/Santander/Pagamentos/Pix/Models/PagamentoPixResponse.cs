using System;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Pix.Models
{
    /// <summary>
    /// Resposta do POST (201), do PATCH (200) e do GET de um pagamento Pix — o Santander
    /// devolve o mesmo objeto nos três, para chave DICT e para QR Code (campos específicos de
    /// cada modalidade vêm nulos na outra).
    /// </summary>
    internal class PagamentoPixResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Textual: PENDING_VALIDATION, READY_TO_PAY, PENDING_CONFIRMATION, PAYED, SCHEDULED
        /// ou REJECTED.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("rejectReason")]
        public string RejectReason { get; set; }

        [JsonProperty("dictCodeType")]
        public string DictCodeType { get; set; }

        [JsonProperty("dictCode")]
        public string DictCode { get; set; }

        [JsonProperty("qrCodeIdentification")]
        public string QrCodeIdentification { get; set; }

        [JsonProperty("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("nominalValue")]
        public decimal NominalValue { get; set; }

        [JsonProperty("paymentValue")]
        public decimal PaymentValue { get; set; }

        [JsonProperty("beneficiary")]
        public PixBeneficiario Beneficiary { get; set; }

        [JsonProperty("transaction")]
        public PixTransacao Transaction { get; set; }
    }

    /// <summary>
    /// Beneficiário do Pix. Na resposta, titular resolvido pelo banco; na requisição, só no Pix
    /// por agência e conta (<c>bankCode</c> ou <c>ispb</c>, nunca os dois). Campos nulos são
    /// omitidos.
    /// </summary>
    internal class PixBeneficiario
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>CPF ou CNPJ.</summary>
        [JsonProperty("documentType", NullValueHandling = NullValueHandling.Ignore)]
        public string DocumentType { get; set; }

        [JsonProperty("documentNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string DocumentNumber { get; set; }

        [JsonProperty("bankCode", NullValueHandling = NullValueHandling.Ignore)]
        public string BankCode { get; set; }

        [JsonProperty("ispb", NullValueHandling = NullValueHandling.Ignore)]
        public string Ispb { get; set; }

        [JsonProperty("branch", NullValueHandling = NullValueHandling.Ignore)]
        public string Branch { get; set; }

        [JsonProperty("number", NullValueHandling = NullValueHandling.Ignore)]
        public string Number { get; set; }

        /// <summary>CONTA_CORRENTE, CONTA_POUPANCA ou CONTA_PAGAMENTO.</summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }

    internal class PixTransacao
    {
        [JsonProperty("date")]
        public DateTime? Date { get; set; }
    }
}

using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Pix.Models
{
    /// <summary>
    /// Corpo da iniciação de pagamento (POST .../pix_payments). Serve para chave DICT
    /// (<see cref="DictCodeType"/> + <see cref="DictCode"/>), agência e conta
    /// (<see cref="Beneficiary"/>) e QR Code (<see cref="QrCode"/>); os campos não usados pela
    /// modalidade ficam nulos e são omitidos.
    /// </summary>
    internal class RequisicaoPagamentoPix
    {
        [JsonProperty("remittanceInformation", NullValueHandling = NullValueHandling.Ignore)]
        public string RemittanceInformation { get; set; }

        [JsonProperty("dictCodeType", NullValueHandling = NullValueHandling.Ignore)]
        public string DictCodeType { get; set; }

        [JsonProperty("dictCode", NullValueHandling = NullValueHandling.Ignore)]
        public string DictCode { get; set; }

        [JsonProperty("beneficiary", NullValueHandling = NullValueHandling.Ignore)]
        public PixBeneficiario Beneficiary { get; set; }

        [JsonProperty("qrCode", NullValueHandling = NullValueHandling.Ignore)]
        public string QrCode { get; set; }

        [JsonProperty("paymentValue")]
        public decimal PaymentValue { get; set; }

        [JsonProperty("paymentDate", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? PaymentDate { get; set; }
    }
}

using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Models
{
    /// <summary>
    /// Corpo da iniciação de pagamento por código (POST .../bank_slip_payments e
    /// .../barcode_payments): código de barras ou linha digitável, e a data só quando futura
    /// (agendamento). O id é omitido para o banco gerar.
    /// </summary>
    internal class RequisicaoPagamentoCodigo
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("paymentDate", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? PaymentDate { get; set; }
    }
}

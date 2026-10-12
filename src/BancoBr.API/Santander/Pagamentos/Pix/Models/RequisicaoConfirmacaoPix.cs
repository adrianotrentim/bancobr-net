using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Pix.Models
{
    /// <summary>
    /// Corpo da efetivação do pagamento (PATCH .../pix_payments/{id}, e também .../bank_slip_payments
    /// e .../barcode_payments, que exigem o <see cref="FinalPayer"/>). Sem
    /// <c>debitAccount</c>, o banco debita a conta principal da workspace.
    /// </summary>
    internal class RequisicaoConfirmacaoPix
    {
        [JsonProperty("paymentValue")]
        public decimal PaymentValue { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = "AUTHORIZED";

        [JsonProperty("debitAccount", NullValueHandling = NullValueHandling.Ignore)]
        public ContaDebito DebitAccount { get; set; }

        /// <summary>Pagador final: obrigatório em boleto e código de barras, e deve repetir o titular da workspace.</summary>
        [JsonProperty("finalPayer", NullValueHandling = NullValueHandling.Ignore)]
        public PixBeneficiario FinalPayer { get; set; }
    }
}

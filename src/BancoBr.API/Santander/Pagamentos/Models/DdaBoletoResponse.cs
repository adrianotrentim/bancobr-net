using System;
using System.Collections.Generic;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Models
{
    /// <summary>Página do GET /workspaces/{id}/authorized_direct_debits (boletos DDA).</summary>
    internal class DdaListResponse
    {
        [JsonProperty("_limit")]
        public int Limit { get; set; }

        [JsonProperty("_pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("_pageElements")]
        public int PageElements { get; set; }

        [JsonProperty("_content")]
        public List<DdaBoletoResponse> Content { get; set; }
    }

    internal class DdaBoletoResponse
    {
        /// <summary>Código de barras (ou linha digitável, conforme a documentação) do boleto.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>Id do título na Nuclea (CIP).</summary>
        [JsonProperty("authorizedDirectDebitId")]
        public string AuthorizedDirectDebitId { get; set; }

        [JsonProperty("bankIdentificationNumber")]
        public string BankIdentificationNumber { get; set; }

        [JsonProperty("kindCode")]
        public string KindCode { get; set; }

        [JsonProperty("dueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("paymentLimitDate")]
        public string PaymentLimitDate { get; set; }

        [JsonProperty("nominalValue")]
        public decimal? NominalValue { get; set; }

        [JsonProperty("payerDocumentNumber")]
        public string PayerDocumentNumber { get; set; }

        /// <summary>"Seu número" do boleto, informado pelo beneficiário.</summary>
        [JsonProperty("beneficiaryIdentificationNumber")]
        public string BeneficiaryIdentificationNumber { get; set; }

        [JsonProperty("beneficiary")]
        public PixBeneficiario Beneficiary { get; set; }

        // A documentação lista o beneficiário também em campos soltos (beneficiaryType/Document/Name).
        [JsonProperty("beneficiaryType")]
        public string BeneficiaryType { get; set; }

        [JsonProperty("beneficiaryDocument")]
        public string BeneficiaryDocument { get; set; }

        [JsonProperty("beneficiaryName")]
        public string BeneficiaryName { get; set; }
    }
}

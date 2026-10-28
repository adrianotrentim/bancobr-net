using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Models
{
    /// <summary>Página do GET /workspaces/{id}/authorized_direct_debits (boletos DDA).</summary>
    internal class DdaListResponse
    {
        [JsonProperty("pageable")]
        public DdaPageable Pageable { get; set; }

        [JsonProperty("content")]
        public List<DdaBoletoResponse> Content { get; set; }
    }

    internal class DdaPageable
    {
        [JsonProperty("_limit")]
        public int Limit { get; set; }

        [JsonProperty("_pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("_pageElements")]
        public int PageElements { get; set; }
    }

    internal class DdaBeneficiarioResponse
    {
        [JsonProperty("beneficiaryType")]
        public string Type { get; set; }

        [JsonProperty("beneficiaryDocument")]
        public string Document { get; set; }

        [JsonProperty("beneficiaryName")]
        public string Name { get; set; }

        /// <summary>"Seu número" do boleto, informado pelo beneficiário.</summary>
        [JsonProperty("beneficiaryIdentificationNumber")]
        public string IdentificationNumber { get; set; }
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

        /// <summary>Valor em texto no formato pt-BR (ex.: "748,31").</summary>
        [JsonProperty("nominalValue")]
        public string NominalValue { get; set; }

        [JsonProperty("payerDocumentType")]
        public string PayerDocumentType { get; set; }

        [JsonProperty("payerDocumentNumber")]
        public string PayerDocumentNumber { get; set; }

        [JsonProperty("payerName")]
        public string PayerName { get; set; }

        [JsonProperty("beneficiary")]
        public DdaBeneficiarioResponse Beneficiary { get; set; }
    }
}

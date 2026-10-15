using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Ted.Models
{
    /// <summary>
    /// Corpo da iniciação da TED (POST .../workspaces/{id}/transfer). Campos nulos são omitidos.
    /// </summary>
    internal class RequisicaoTedSantander
    {
        /// <summary>UUID v4 opcional que identifica o pagamento; reenviar o mesmo id evita duplicar a TED.</summary>
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("sourceAccount")]
        public TedContaOrigem SourceAccount { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("destinationAccount")]
        public TedContaDestino DestinationAccount { get; set; }

        [JsonProperty("identifierTransferCode", NullValueHandling = NullValueHandling.Ignore)]
        public string IdentifierTransferCode { get; set; }

        [JsonProperty("transferHistory", NullValueHandling = NullValueHandling.Ignore)]
        public string TransferHistory { get; set; }

        [JsonProperty("creditOperationContractNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string CreditOperationContractNumber { get; set; }

        [JsonProperty("transferValue")]
        public decimal TransferValue { get; set; }

        /// <summary>Câmara de destino; hoje só STR0008 (TED comum).</summary>
        [JsonProperty("destinationType")]
        public string DestinationType { get; set; } = "STR0008";
    }

    /// <summary>Conta pagadora (agência com até 4 e conta com até 20 posições); serve também no corpo do PATCH.</summary>
    internal class TedContaOrigem
    {
        [JsonProperty("branchCode")]
        public string BranchCode { get; set; }

        [JsonProperty("accountNumber")]
        public string AccountNumber { get; set; }
    }

    /// <summary>Conta e titular do favorecido. Para STR0008 todos os campos são obrigatórios.</summary>
    internal class TedContaDestino
    {
        [JsonProperty("bankCode")]
        public string BankCode { get; set; }

        /// <summary>Só é enviado para conta de pagamento (PG); nas demais o código do banco basta.</summary>
        [JsonProperty("ispbCode", NullValueHandling = NullValueHandling.Ignore)]
        public string IspbCode { get; set; }

        [JsonProperty("branchCode")]
        public string BranchCode { get; set; }

        [JsonProperty("accountNumber")]
        public string AccountNumber { get; set; }

        /// <summary>CC (corrente), PP (poupança) ou PG (pagamento).</summary>
        [JsonProperty("typeAccount")]
        public string TypeAccount { get; set; }

        /// <summary>CPF ou CNPJ.</summary>
        [JsonProperty("legalEntityIdentifier")]
        public string LegalEntityIdentifier { get; set; }

        [JsonProperty("documentIdentifierNumber")]
        public string DocumentIdentifierNumber { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Finalidade da TED, textual (ex.: PAGAMENTO_DE_IMPOSTOS_TRIBUTOS_E_TAXAS).</summary>
        [JsonProperty("purpose")]
        public string Purpose { get; set; }
    }
}

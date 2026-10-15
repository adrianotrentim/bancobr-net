using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Pix.Models
{
    /// <summary>Resposta paginada de GET /workspaces.</summary>
    internal class WorkspaceListResponse
    {
        [JsonProperty("_pageable")]
        public WorkspacePaginacao Pageable { get; set; }

        [JsonProperty("_content")]
        public List<Workspace> Content { get; set; }
    }

    internal class WorkspacePaginacao
    {
        [JsonProperty("_totalElements")]
        public int TotalElements { get; set; }
    }

    /// <summary>
    /// Workspace do Hub de Pagamentos. Serve de leitura (GET), de corpo para criação (POST) e
    /// de corpo para manutenção (PATCH) — por isso todos os campos são omitidos quando nulos.
    /// Modela a workspace inteira de propósito: ao habilitar o Pix numa workspace existente, o
    /// PATCH reenvia tudo o que o GET devolveu (contas adicionais e demais flags de produto),
    /// senão o banco removeria/zeraria o que não for enviado.
    /// </summary>
    internal class Workspace
    {
        // A documentação usa "id" na criação/consulta por ID e "workspace_id" na listagem.
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty("workspace_id", NullValueHandling = NullValueHandling.Ignore)]
        public string WorkspaceId { get; set; }

        [JsonIgnore]
        public string IdResolvido => Id ?? WorkspaceId;

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("mainDebitAccount", NullValueHandling = NullValueHandling.Ignore)]
        public ContaDebito MainDebitAccount { get; set; }

        // As tabelas da documentação grafam "addicionalDebitAccounts", mas os exemplos JSON usam
        // "additionalDebitAccounts". Lê os dois (perder as contas adicionais num PATCH as
        // removeria da workspace); só o segundo é enviado.
        [JsonProperty("additionalDebitAccounts", NullValueHandling = NullValueHandling.Ignore)]
        public List<ContaDebito> AdditionalDebitAccounts { get; set; }

        [JsonProperty("addicionalDebitAccounts", NullValueHandling = NullValueHandling.Ignore)]
        public List<ContaDebito> AddicionalDebitAccounts { get; set; }

        [JsonProperty("webhookURL", NullValueHandling = NullValueHandling.Ignore)]
        public string WebhookUrl { get; set; }

        [JsonProperty("pixPaymentsActive")]
        public bool PixPaymentsActive { get; set; }

        [JsonProperty("bankSlipPaymentsActive")]
        public bool BankSlipPaymentsActive { get; set; }

        [JsonProperty("bankSlipAvailableActive")]
        public bool BankSlipAvailableActive { get; set; }

        [JsonProperty("barCodePaymentsActive")]
        public bool BarCodePaymentsActive { get; set; }

        [JsonProperty("taxesByFieldPaymentsActive")]
        public bool TaxesByFieldPaymentsActive { get; set; }

        [JsonProperty("vehicleTaxesPaymentsActive")]
        public bool VehicleTaxesPaymentsActive { get; set; }

        [JsonProperty("bankSlipAvailableWebhookActive")]
        public bool BankSlipAvailableWebhookActive { get; set; }

        [JsonProperty("bankTransferPaymentsActive")]
        public bool BankTransferPaymentsActive { get; set; }

        [JsonProperty("smartTransfersActive")]
        public bool SmartTransfersActive { get; set; }
    }

    internal class ContaDebito
    {
        [JsonProperty("branch")]
        public string Branch { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }
    }
}

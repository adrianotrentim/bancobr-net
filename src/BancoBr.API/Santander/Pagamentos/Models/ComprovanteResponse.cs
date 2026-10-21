using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Pagamentos.Models
{
    /// <summary>
    /// Só o que o comprovante precisa de qualquer consulta de pagamento (Pix, TED, boleto ou
    /// código de barras): o status e o <c>transaction.code</c>, que identifica o pagamento na
    /// API de comprovantes. O resto da resposta varia por modalidade e é ignorado.
    /// </summary>
    internal class PagamentoTransacaoResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("transaction")]
        public PagamentoTransacao Transaction { get; set; }
    }

    internal class PagamentoTransacao
    {
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>Resposta do GET <c>file_requests</c> (lista de solicitações do comprovante de um pagamento).</summary>
    internal class ComprovanteListaResponse
    {
        [JsonProperty("paymentReceiptsFileRequests")]
        public List<ComprovanteResponse> PaymentReceiptsFileRequests { get; set; }
    }

    /// <summary>
    /// Solicitação de arquivo de comprovante (consult_payment_receipts v2): resposta do POST
    /// <c>file_requests</c>, do GET por <c>request_id</c> e cada item do GET da lista. Enquanto
    /// o arquivo não fica pronto, <c>file.statusInfo.statusCode</c> é REQUESTED e o link vem
    /// nulo; pronto, vira AVAILABLE e <c>file.fileRepository.location</c> traz o link.
    /// </summary>
    internal class ComprovanteResponse
    {
        [JsonProperty("request")]
        public ComprovanteSolicitacao Request { get; set; }

        [JsonProperty("file")]
        public ComprovanteArquivo File { get; set; }
    }

    internal class ComprovanteSolicitacao
    {
        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("creationDateTime")]
        public DateTime? CreationDateTime { get; set; }
    }

    internal class ComprovanteArquivo
    {
        [JsonProperty("fileRepository")]
        public ComprovanteRepositorio FileRepository { get; set; }

        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        /// <summary>Validade do link (curta: poucos minutos depois de gerado).</summary>
        [JsonProperty("expirationDate")]
        public DateTime? ExpirationDate { get; set; }

        [JsonProperty("statusInfo")]
        public ComprovanteStatus StatusInfo { get; set; }
    }

    internal class ComprovanteRepositorio
    {
        [JsonProperty("location")]
        public string Location { get; set; }
    }

    internal class ComprovanteStatus
    {
        /// <summary>REQUESTED (em geração) ou AVAILABLE (link pronto).</summary>
        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }
    }

    /// <summary>Corpo do POST <c>file_requests</c>: <c>{"payment":{"requestValueDate":"yyyy-MM"}}</c>.</summary>
    internal class RequisicaoComprovante
    {
        [JsonProperty("payment")]
        public RequisicaoComprovantePagamento Payment { get; set; }
    }

    internal class RequisicaoComprovantePagamento
    {
        /// <summary>Ano e mês no formato <c>yyyy-MM</c>.</summary>
        [JsonProperty("requestValueDate")]
        public string RequestValueDate { get; set; }
    }
}

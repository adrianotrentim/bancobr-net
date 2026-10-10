using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Santander.Errors
{
    /// <summary>
    /// Corpo de erro padrão do Hub de Pagamentos do Santander (mesmo formato para todos os
    /// status HTTP de erro).
    /// </summary>
    internal class SantanderErrorResponse
    {
        [JsonProperty("_errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("_message")]
        public string Message { get; set; }

        [JsonProperty("_details")]
        public string Details { get; set; }

        [JsonProperty("_traceId")]
        public string TraceId { get; set; }

        [JsonProperty("_errors")]
        public List<SantanderErrorItem> Errors { get; set; }
    }

    internal class SantanderErrorItem
    {
        [JsonProperty("_code")]
        public string Code { get; set; }

        [JsonProperty("_field")]
        public string Field { get; set; }

        [JsonProperty("_message")]
        public string Message { get; set; }
    }
}

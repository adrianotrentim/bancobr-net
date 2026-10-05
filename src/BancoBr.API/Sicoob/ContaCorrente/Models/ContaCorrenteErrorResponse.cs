using System.Collections.Generic;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.ContaCorrente.Models
{
    /// <summary>
    /// Envelope de erro da API Conta Corrente (ex.: GET /extrato/{mes}/{ano}):
    /// { "errors": [{ "code", "title", "detail" }], "meta": { "requestDateTime" } }.
    /// </summary>
    internal class ContaCorrenteErrorResponse
    {
        [JsonProperty("errors")]
        public List<ContaCorrenteErrorItem> Errors { get; set; }
    }
}
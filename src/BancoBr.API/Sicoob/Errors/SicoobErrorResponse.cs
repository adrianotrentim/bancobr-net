using System.Collections.Generic;
using BancoBr.API.Core.Errors;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Errors
{
    internal class SicoobErrorResponse
    {
        [JsonProperty("mensagens")]
        public List<MensagemErro> Mensagens { get; set; } = new List<MensagemErro>();
    }
}

using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Corpo da requisição de iniciação de pagamento por chave DICT (POST /pagamentos).
    /// </summary>
    internal class RequisicaoIniciacaoPix
    {
        [JsonProperty("chave")]
        public string Chave { get; set; }

        [JsonProperty("dataAgendamento", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? DataAgendamento { get; set; }
    }
}

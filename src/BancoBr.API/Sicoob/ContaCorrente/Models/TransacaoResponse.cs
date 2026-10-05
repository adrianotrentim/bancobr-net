using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.ContaCorrente.Models
{
    /// <summary>
    /// Item de "transacoes" devolvido por GET /conta-corrente/v4/extrato/{mes}/{ano}.
    /// </summary>
    internal class TransacaoResponse
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; }

        [JsonProperty("valor")]
        public string Valor { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("dataLote")]
        public string DataLote { get; set; }

        [JsonProperty("descricao")]
        public string Descricao { get; set; }

        [JsonProperty("numeroDocumento")]
        public string NumeroDocumento { get; set; }

        [JsonProperty("cpfCnpj")]
        public string CpfCnpj { get; set; }

        [JsonProperty("descInfComplementar")]
        public string DescInfComplementar { get; set; }
    }
}

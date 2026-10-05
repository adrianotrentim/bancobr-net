using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.ContaCorrente.Models
{
    /// <summary>
    /// Corpo de resposta de GET /conta-corrente/v4/extrato/{mes}/{ano} — ao contrário da API
    /// de pagamentos, não vem envelopado em "resultado".
    /// </summary>
    internal class ExtratoResponse
    {
        [JsonProperty("saldoAtual")]
        public string SaldoAtual { get; set; }

        [JsonProperty("saldoBloqueado")]
        public string SaldoBloqueado { get; set; }

        [JsonProperty("saldoLimite")]
        public string SaldoLimite { get; set; }

        [JsonProperty("saldoAnterior")]
        public string SaldoAnterior { get; set; }

        [JsonProperty("saldoBloqueioJudicial")]
        public string SaldoBloqueioJudicial { get; set; }

        [JsonProperty("saldoBloqueioJudicialAnterior")]
        public string SaldoBloqueioJudicialAnterior { get; set; }

        [JsonProperty("transacoes")]
        public TransacaoResponse[] Transacoes { get; set; }
    }
}

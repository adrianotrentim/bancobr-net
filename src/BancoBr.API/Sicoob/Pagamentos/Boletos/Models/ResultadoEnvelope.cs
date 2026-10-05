using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// O Sicoob envelopa as respostas de consulta/pagamento em { "resultado": ... }.
    /// Usado apenas internamente pelo PagamentoBoletoClient para desserialização.
    /// </summary>
    internal class ResultadoEnvelope<T>
    {
        [JsonProperty("resultado")]
        public T Resultado { get; set; }
    }
}

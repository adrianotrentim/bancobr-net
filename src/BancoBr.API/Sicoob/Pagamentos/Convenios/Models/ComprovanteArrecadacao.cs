using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Resultado de GET /arrecadacao/pagamentos/{nsu}/comprovante: segunda via do comprovante.
    /// </summary>
    internal class ComprovanteArrecadacao
    {
        [JsonProperty("comprovante")]
        public string Comprovante { get; set; }

        [JsonProperty("pagamento")]
        public Arrecadacao Pagamento { get; set; }
    }
}

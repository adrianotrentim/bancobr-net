using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Resultado de POST /arrecadacao/codigo-barras/{codigoBarras}/pagamentos (HTTP 200).
    /// </summary>
    internal class ArrecadacaoResultado
    {
        /// <summary>Comprovante de pagamento em Base64.</summary>
        [JsonProperty("comprovante")]
        public string Comprovante { get; set; }

        [JsonProperty("arrecadacao")]
        public Arrecadacao Arrecadacao { get; set; }
    }
}

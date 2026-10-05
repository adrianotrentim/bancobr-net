using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Corpo de POST /arrecadacao/codigo-barras/{codigoBarras}/pagamentos.
    /// </summary>
    internal class ArrecadacaoPagamentoRequest
    {
        [JsonProperty("identificacao")]
        public Identificacao Identificacao { get; set; }

        [JsonProperty("pagamento")]
        public PagamentoConvenio Pagamento { get; set; }

        [JsonProperty("transacao")]
        public long Transacao { get; set; }
    }
}

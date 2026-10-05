using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Item retornado por GET /arrecadacao/convenios-habilitados.
    /// </summary>
    internal class ConvenioHabilitado
    {
        [JsonProperty("identificador")]
        public string Identificador { get; set; }

        [JsonProperty("sigla")]
        public string Sigla { get; set; }

        [JsonProperty("codigoFebraban")]
        public string CodigoFebraban { get; set; }

        [JsonProperty("segmento")]
        public int Segmento { get; set; }
    }
}

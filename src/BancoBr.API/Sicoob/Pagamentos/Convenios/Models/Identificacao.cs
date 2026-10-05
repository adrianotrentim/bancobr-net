using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Identificação da instituição e unidade correspondente ao pagamento de convênio.
    /// </summary>
    internal class Identificacao
    {
        [JsonProperty("instituicao")]
        public long Instituicao { get; set; }

        [JsonProperty("unidade")]
        public int Unidade { get; set; }
    }
}

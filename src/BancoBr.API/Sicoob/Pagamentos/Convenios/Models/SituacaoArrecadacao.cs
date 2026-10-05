using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>Situação da arrecadação retornada em ArrecadacaoConsultaItem.Situacao.</summary>
    internal class SituacaoArrecadacao
    {
        [JsonProperty("codigo")]
        public int Codigo { get; set; }

        [JsonProperty("descricao")]
        public string Descricao { get; set; }
    }
}

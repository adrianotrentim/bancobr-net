using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Corpo da requisição de efetivação de um pagamento Pix já iniciado (POST /pagamentos/confirmacao).
    /// </summary>
    internal class RequisicaoEfetivacaoPix
    {
        [JsonProperty("endToEndId")]
        public string EndToEndId { get; set; }

        /// <summary>
        /// String, não decimal: o Sicoob exige o padrão ^[0-9]{1,18}([,][0-9]{1,2})?$
        /// (vírgula como separador decimal), ex.: "150,00".
        /// </summary>
        [JsonProperty("valor")]
        public string Valor { get; set; }

        [JsonProperty("descricao", NullValueHandling = NullValueHandling.Ignore)]
        public string Descricao { get; set; }

        [JsonProperty("meioIniciacao")]
        public string MeioIniciacao { get; set; } = "CHAVE";

        [JsonProperty("dataAgendamento", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? DataAgendamento { get; set; }
    }
}

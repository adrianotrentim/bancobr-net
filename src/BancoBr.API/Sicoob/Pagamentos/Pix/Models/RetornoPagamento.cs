using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Retorno da consulta (GET /pagamentos/{id}) e da confirmação (POST /pagamentos/confirmacao)
    /// de um pagamento Pix no Sicoob.
    /// </summary>
    internal class RetornoPagamento
    {
        [JsonProperty("endToEndId")]
        public string EndToEndId { get; set; }

        /// <summary>
        /// Mantido como string em vez de enum: a documentação do Sicoob usa valores
        /// diferentes para o mesmo conceito (ex.: EM_PROCESSAMENTO/FINALIZADO/REJEITADO
        /// na consulta, mas FINALIZADO_SUCESSO/FINALIZADO_REJEICAO no webhook).
        /// </summary>
        [JsonProperty("estado")]
        public string Estado { get; set; }

        [JsonProperty("valor")]
        public decimal Valor { get; set; }

        [JsonProperty("detalheRejeicao")]
        public string DetalheRejeicao { get; set; }

        [JsonProperty("descricao")]
        public string Descricao { get; set; }

        [JsonProperty("horario")]
        public DateTime? Horario { get; set; }

        [JsonProperty("origem")]
        public DadosContaUsuario Origem { get; set; }

        [JsonProperty("destino")]
        public DadosContaUsuario Destino { get; set; }

        [JsonProperty("dataAgendamento")]
        public DateTime? DataAgendamento { get; set; }
    }
}

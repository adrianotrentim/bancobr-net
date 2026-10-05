using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>Item de "resultado" devolvido por DELETE /transferencias/agendamentos/{idAgendamento}.</summary>
    internal class TedCancelamentoResultado
    {
        [JsonProperty("mensagem")]
        public string Mensagem { get; set; }
    }
}

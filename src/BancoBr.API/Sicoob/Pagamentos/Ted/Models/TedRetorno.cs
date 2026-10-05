using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Ted.Models
{
    /// <summary>
    /// Corpo devolvido pelo envio (POST /transferencias, sem envelope) e pela consulta
    /// (GET /transferencias/{codigo}, dentro de "resultado": [...]).
    /// </summary>
    internal class TedRetorno
    {
        [JsonProperty("finalidade")]
        public string Finalidade { get; set; }

        [JsonProperty("numeroPa")]
        public string NumeroPa { get; set; }

        [JsonProperty("numeroControleIF")]
        public string NumeroControleIF { get; set; }

        [JsonProperty("mensagemErro")]
        public string MensagemErro { get; set; }

        [JsonProperty("idAgendamento")]
        public long IdAgendamento { get; set; }

        [JsonProperty("agendamento")]
        public bool Agendamento { get; set; }

        /// <summary>Propriedade com cedilha no wire do Sicoob ("situação").</summary>
        [JsonProperty("situação")]
        public string Situacao { get; set; }

        [JsonProperty("historico")]
        public string Historico { get; set; }

        [JsonProperty("tipoPessoaDebito")]
        public string TipoPessoaDebito { get; set; }

        [JsonProperty("nomePessoaDebito")]
        public string NomePessoaDebito { get; set; }

        [JsonProperty("numeroCPFCNPJDebito")]
        public string NumeroCPFCNPJDebito { get; set; }

        [JsonProperty("numeroBancoFavorecido")]
        public string NumeroBancoFavorecido { get; set; }

        [JsonProperty("codigoSituacaoAgendamento")]
        public int CodigoSituacaoAgendamento { get; set; }

        [JsonProperty("debtorAccount")]
        public DebtorAccount DebtorAccount { get; set; }

        [JsonProperty("creditorAccount")]
        public CreditorAccount CreditorAccount { get; set; }

        [JsonProperty("creditor")]
        public Creditor Creditor { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }
    }
}

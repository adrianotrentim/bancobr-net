using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Dados de conta (origem ou destino) retornados pela consulta/confirmação de um
    /// pagamento Pix no Sicoob.
    /// </summary>
    internal class DadosContaUsuario
    {
        [JsonProperty("ispb")]
        public string Ispb { get; set; }

        [JsonProperty("cpfCnpj")]
        public string CpfCnpj { get; set; }

        [JsonProperty("nome")]
        public string Nome { get; set; }

        [JsonProperty("conta")]
        public string Conta { get; set; }

        [JsonProperty("agencia")]
        public string Agencia { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; }

        [JsonProperty("chaveDict")]
        public string ChaveDict { get; set; }

        [JsonProperty("boolFavorecido")]
        public bool? BoolFavorecido { get; set; }
    }
}

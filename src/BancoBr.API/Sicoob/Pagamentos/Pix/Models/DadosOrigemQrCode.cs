using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Dados do pagador exigidos pelo pagamento via QR Code (POST /pagamentos/qrcode).
    /// Diferente de <see cref="DadosContaUsuario"/> (retornado pela consulta/confirmação),
    /// este endpoint não aceita "chaveDict"/"boolFavorecido" no objeto "origem".
    /// </summary>
    internal class DadosOrigemQrCode
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
    }
}

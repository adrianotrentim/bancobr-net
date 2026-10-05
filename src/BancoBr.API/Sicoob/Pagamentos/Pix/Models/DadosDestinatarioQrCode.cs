using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Dados do destinatário exigidos pelo pagamento via QR Code (POST /pagamentos/qrcode)
    /// — apenas o CPF/CNPJ, usado pelo Sicoob para validar contra o payload do QR Code.
    /// </summary>
    internal class DadosDestinatarioQrCode
    {
        [JsonProperty("cpfCnpj")]
        public string CpfCnpj { get; set; }
    }
}

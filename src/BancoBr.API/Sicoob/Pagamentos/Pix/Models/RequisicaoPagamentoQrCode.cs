using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Corpo da requisição de pagamento via QR Code (POST /pagamentos/qrcode).
    /// </summary>
    internal class RequisicaoPagamentoQrCode
    {
        [JsonProperty("qrcode")]
        public string QrCode { get; set; }

        [JsonProperty("codMun", NullValueHandling = NullValueHandling.Ignore)]
        public string CodMun { get; set; }

        [JsonProperty("repeticao")]
        public bool Repeticao { get; set; }

        [JsonProperty("dataVencimento", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? DataVencimento { get; set; }

        [JsonProperty("dataAgendamento", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime? DataAgendamento { get; set; }

        /// <summary>
        /// String, não decimal: o Sicoob exige o padrão ^((\d{1,16}\.\d{2}))$ (ponto como
        /// separador decimal), ex.: "150.00" — diferente do endpoint de confirmação por
        /// chave, que usa vírgula.
        /// </summary>
        [JsonProperty("valor")]
        public string Valor { get; set; }

        [JsonProperty("pagarComJurosMulta")]
        public bool PagarComJurosMulta { get; set; }

        [JsonProperty("origem")]
        public DadosOrigemQrCode Origem { get; set; }

        [JsonProperty("destino")]
        public DadosDestinatarioQrCode Destino { get; set; }
    }
}

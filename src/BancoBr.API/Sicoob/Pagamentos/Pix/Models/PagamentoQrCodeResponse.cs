using System;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Pix.Models
{
    /// <summary>
    /// Retorno do pagamento via QR Code (POST /pagamentos/qrcode) no Sicoob.
    /// </summary>
    internal class PagamentoQrCodeResponse
    {
        [JsonProperty("endToEndId")]
        public string EndToEndId { get; set; }

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

        [JsonProperty("txId")]
        public string TxId { get; set; }

        [JsonProperty("valorOriginal")]
        public decimal? ValorOriginal { get; set; }

        [JsonProperty("abatimento")]
        public decimal? Abatimento { get; set; }

        [JsonProperty("desconto")]
        public decimal? Desconto { get; set; }

        [JsonProperty("multa")]
        public decimal? Multa { get; set; }

        [JsonProperty("juros")]
        public decimal? Juros { get; set; }

        [JsonProperty("tipoQrcode")]
        public string TipoQrcode { get; set; }
    }
}

using System;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Dados de pagamento enviados no corpo de POST /arrecadacao/codigo-barras/{codigoBarras}/pagamentos.
    /// </summary>
    internal class PagamentoConvenio
    {
        [JsonProperty("valorPago")]
        public decimal ValorPago { get; set; }

        /// <summary>Número Sequencial Único. Obrigatório apenas para o perfil "Parceiro Banco Sicoob".</summary>
        [JsonProperty("nsu")]
        public long? Nsu { get; set; }

        [JsonProperty("dataPagamento")]
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime DataPagamento { get; set; }

        [JsonProperty("valorDocumento")]
        public decimal ValorDocumento { get; set; }

        [JsonProperty("valorDesconto")]
        public decimal ValorDesconto { get; set; }

        [JsonProperty("valorJuros")]
        public decimal ValorJuros { get; set; }

        [JsonProperty("valorMulta")]
        public decimal ValorMulta { get; set; }

        /// <summary>Uso exclusivo do perfil "Parceiro Banco Sicoob".</summary>
        [JsonProperty("recebimentoViaCaixa")]
        public bool? RecebimentoViaCaixa { get; set; }
    }
}

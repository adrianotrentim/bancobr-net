using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.BancoDoBrasil.Http;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.BancoDoBrasil.Pagamentos
{
    /// <summary>Pagamentos em Lote v1: o aceite dos dados não significa liquidação. A consulta posterior informa o estado do lançamento.</summary>
    internal class PagamentosClient
    {
        private readonly BancoDoBrasilHttpGateway _gateway;
        private readonly Correntista _origem;
        private readonly string _baseUrl;
        internal PagamentosClient(BancoDoBrasilHttpGateway gateway, Correntista origem, Uri baseUrl)
        {
            _gateway = gateway;
            _origem = origem ?? throw new ArgumentNullException(nameof(origem));
            _baseUrl = (baseUrl ?? BancoDoBrasilHttpGateway.PagamentosBaseUrl).ToString().TrimEnd('/') + "/";
        }

        /// <summary>O convênio de pagamentos da conta identifica o contrato BB no protocolo.</summary>
        private int? ObterContrato()
        {
            if (string.IsNullOrWhiteSpace(_origem.Convenio)) return null;
            if (!int.TryParse(_origem.Convenio, NumberStyles.None, CultureInfo.InvariantCulture, out var contrato) || contrato <= 0)
                throw new ArgumentException("Correntista.Convenio deve conter um número inteiro positivo para os pagamentos BB.", nameof(Correntista.Convenio));
            return contrato;
        }

        internal static int NumeroRequisicao(Movimento movimento)
        {
            if (!int.TryParse(movimento.NumeroDocumento, NumberStyles.None, CultureInfo.InvariantCulture, out var numero) || numero < 1 || numero > 9999999)
                throw new ArgumentException("O BB exige NumeroDocumento como número de requisição único entre 1 e 9999999. Preserve-o nos reenvios.", nameof(movimento));
            return numero;
        }

        internal async Task<Movimento> EnviarAsync(Movimento movimento, string recurso, JObject lancamento, CancellationToken ct)
        {
            if (movimento.ValorPagamento <= 0 || movimento.DataPagamento == default)
                throw new ArgumentException("Informe valor positivo e data de pagamento.", nameof(movimento));
            var codigo = recurso == "lotes-boletos" || recurso == "lotes-guias-codigo-barras";
            var corpo = new JObject { ["numeroRequisicao"] = NumeroRequisicao(movimento) };
            corpo[codigo ? "numeroAgenciaDebito" : "agenciaDebito"] = _origem.NumeroAgencia;
            corpo[codigo ? "numeroContaCorrenteDebito" : "contaCorrenteDebito"] = _origem.NumeroConta;
            corpo[codigo ? "digitoVerificadorContaCorrenteDebito" : "digitoVerificadorContaCorrente"] = _origem.DVConta;
            var contrato = ObterContrato();
            if (contrato.HasValue) corpo[codigo ? "codigoContrato" : recurso == "lotes-transferencias-pix" ? "numeroContrato" : "numeroContratoPagamento"] = contrato.Value;
            if (!codigo) corpo["tipoPagamento"] = 128;
            corpo[codigo ? "lancamentos" : "listaTransferencias"] = new JArray(lancamento);
            var resposta = await _gateway.SendAsync<JObject>(HttpMethod.Post, _baseUrl + recurso, corpo, ct).ConfigureAwait(false);
            var item = (resposta?[codigo ? "lancamentos" : recurso == "lotes-transferencias-pix" ? "listaTransferencias" : "transferencias"] as JArray)?.FirstOrDefault() as JObject;
            if (item == null) throw new InvalidOperationException("O BB não retornou o lançamento solicitado.");
            movimento.NumeroDocumentoNoBanco = (string)(item["codigoIdentificadorPagamento"] ?? item["identificadorPagamento"] ?? item["identificadorTransferencia"]);
            var aceite = (string)(item["indicadorAceite"] ?? item["indicadorMovimentoAceito"]);
            var erros = item["errorCodes"] ?? item["erros"] ?? item["errors"];
            movimento.DetalheRejeicaoBancoBr = erros == null ? null : string.Join(", ", erros.Children().Select(e => e.Type == JTokenType.Object ? (string)(e["codigo"] ?? e["mensagem"]) : e.ToString()).Where(e => e != "0"));
            var estado = (int?)(resposta["estadoRequisicao"] ?? resposta["codigoEstado"]);
            movimento.SituacaoBancoBr = aceite == "N" || estado == 3 || estado == 7 ? BancoBrSituacaoEnum.Rejeitado : BancoBrSituacaoEnum.NaoIntegrado;
            return movimento;
        }

        internal async Task<Movimento> ConsultarAsync(Movimento movimento, string recurso, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco)) throw new ArgumentException("Informe o identificador do lançamento no BB.", nameof(movimento));
            var resposta = await _gateway.SendAsync<JObject>(HttpMethod.Get, _baseUrl + recurso + "/" + Uri.EscapeDataString(movimento.NumeroDocumentoNoBanco), null, ct).ConfigureAwait(false);
            switch (((string)resposta["estadoPagamento"])?.Trim().ToUpperInvariant())
            {
                case "PAGO": movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Efetivado; break;
                case "AGENDADO": movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Agendado; break;
                case "CANCELADO": movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado; break;
                case "REJEITADO": case "INCONSISTENTE": case "DEVOLVIDO": case "BLOQUEADO": movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Rejeitado; break;
                default: movimento.SituacaoBancoBr = BancoBrSituacaoEnum.NaoIntegrado; break;
            }
            if (resposta["valorPagamento"] != null) movimento.ValorPagamento = (decimal)resposta["valorPagamento"];
            if (DateTime.TryParseExact(((string)resposta["dataPagamento"])?.PadLeft(8, '0'), "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)) movimento.HorarioBancoBr = data;
            var autenticacao = (string)(resposta["codigoAutenticacaoPagamento"] ?? resposta["autenticacaoPagamento"]);
            if (movimento.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra boleto) boleto.NumeroAutenticacaoPagamento = autenticacao;
            if (movimento.MovimentoItem is MovimentoItemTransferenciaTED ted) ted.CodigoAutenticacao = autenticacao;
            if (movimento.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra convenio) convenio.Autenticacao = autenticacao;
            return movimento;
        }

        internal async Task<Movimento> CancelarAsync(Movimento movimento, CancellationToken ct)
        {
            if (!long.TryParse(movimento.NumeroDocumentoNoBanco, out var id)) throw new ArgumentException("Informe identificador numérico do pagamento BB.");
            var corpo = new JObject { ["agenciaDebito"] = _origem.NumeroAgencia, ["contaCorrenteDebito"] = _origem.NumeroConta, ["digitoVerificadorContaCorrente"] = _origem.DVConta, ["listaPagamentos"] = new JArray(new JObject { ["codigoPagamento"] = id }) };
            var contrato = ObterContrato();
            if (contrato.HasValue) corpo["numeroContratoPagamento"] = contrato.Value;
            var resposta = await _gateway.SendAsync<JObject>(HttpMethod.Post, _baseUrl + "cancelar-pagamentos", corpo, ct).ConfigureAwait(false);
            var item = resposta["pagamentos"]?.First;
            movimento.DetalheRejeicaoBancoBr = (string)item?["estadoCancelamento"];
            if ((string)item?["indicadorCancelamento"] == "S") movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado;
            return movimento;
        }

        internal static int Data(DateTime data) => int.Parse(data.ToString("ddMMyyyy", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        internal static string Digitos(string valor) => new string((valor ?? "").Where(char.IsDigit).ToArray());
        internal static string Codigo(string valor)
        {
            if (valor == null || valor.Length != 44 || valor.Any(c => c < '0' || c > '9')) throw new ArgumentException("O BB exige código de barras com 44 dígitos.");
            return valor;
        }
    }
}

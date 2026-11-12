using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Bradesco.Http;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.Pagamentos
{
    /// <summary>
    /// API de Pagamento de Contas de Consumo e Tributos por código de barras do Bradesco
    /// (<c>pagamento/arrecadacao-via-codbarras/v1</c>): água, luz, telefone, guias de impostos e demais
    /// documentos de arrecadação (código iniciado em 8). Não atende contas de instituições financeiras.
    ///
    /// <para>O pagamento tem duas etapas no mesmo endpoint: <c>tipoRegistro</c> 0 valida os dados
    /// (<see cref="ConsultarCodigoBarrasAsync"/>) e <c>tipoRegistro</c> 1 paga (<see cref="PagarConvenioAsync"/>).
    /// Por isso <see cref="ExigeConsultaPrevia"/> é verdadeiro e a fachada faz a consulta antes de enviar.</para>
    ///
    /// <para>O <c>idTransacao</c> (até 9 caracteres alfanuméricos) vem de <see cref="Movimento.NumeroDocumento"/> e é
    /// o que identifica o pagamento: o banco recusa um <c>idTransacao</c> já utilizado, então reenviar o mesmo
    /// movimento não duplica o pagamento. Fica em <see cref="Movimento.NumeroDocumentoNoBanco"/> após o envio e é
    /// o que <see cref="ConsultarComprovantePorNsuAsync"/> usa para saber a situação (efetuado, agendado ou não efetuado).</para>
    ///
    /// Aceita código de barras (44 dígitos) ou a representação numérica com dígitos verificadores (48 dígitos);
    /// o valor vem de <see cref="Movimento.ValorPagamento"/> ou, se zero, do próprio código quando ele traz o valor.
    /// A API não tem comprovante em PDF, conciliação nem lista de convênios habilitados.
    /// </summary>
    internal class PagamentoConvenioClient : PagamentoConvenioApiBase
    {
        private const string Recurso = "pagamento/arrecadacao-via-codbarras/v1/";
        private const string RetornoSucesso = "000";
        private static readonly Regex IdTransacaoValido = new Regex("^[A-Za-z0-9]{1,9}$", RegexOptions.Compiled);

        private readonly BradescoHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly Func<DateTime> _relogio;

        /// <param name="relogio">Data de Brasília usada para decidir entre efetuado e agendado; por padrão, o relógio local.</param>
        internal PagamentoConvenioClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, Correntista origem, Uri baseUrl = null, Func<DateTime> relogio = null)
            : base(httpClient, origem)
        {
            _gateway = new BradescoHttpGateway(httpClient, tokenProvider);
            _baseUrl = BradescoHttpGateway.NormalizarBase(baseUrl ?? BradescoHttpGateway.PagamentosBaseUrl) + Recurso;
            _relogio = relogio ?? (() => DateTime.Now);
        }

        /// <summary>Etapa 1 (<c>tipoRegistro</c> 0): valida e devolve valores, vencimento e a empresa conveniada.</summary>
        internal override async Task<Movimento> ConsultarCodigoBarrasAsync(Movimento movimento, bool? recebimentoViaCaixa = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var resposta = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "pagamentoContaConsumo", MontarRequisicao(movimento, item, tipoRegistro: 0, exigirId: false), cancellationToken).ConfigureAwait(false);
            ValidarRetorno(resposta);

            AplicarRegistro(movimento, item, Registro(resposta, item));
            movimento.Situacao = BancoBrSituacaoEnum.NaoIntegrado;
            movimento.DetalheRejeicao = null;
            return movimento;
        }

        /// <summary>Etapa 2 (<c>tipoRegistro</c> 1): paga. Com data futura o banco agenda o débito.</summary>
        internal override async Task<Movimento> PagarConvenioAsync(Movimento movimento, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var corpo = MontarRequisicao(movimento, item, tipoRegistro: 1, exigirId: true);
            var resposta = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "pagamentoContaConsumo", corpo, cancellationToken).ConfigureAwait(false);
            ValidarRetorno(resposta);

            var registro = Registro(resposta, item);
            AplicarRegistro(movimento, item, registro);
            movimento.NumeroDocumentoNoBanco = IdTransacao(movimento);

            var dataDebito = DataIso(registro?["dataDebito"]) ?? DataDebito(movimento);
            movimento.Situacao = dataDebito.Date > _relogio().Date ? BancoBrSituacaoEnum.Agendado : BancoBrSituacaoEnum.Efetivado;
            movimento.DetalheRejeicao = null;
            return movimento;
        }

        /// <summary>
        /// Descobre a situação pelo <c>idTransacao</c> em <see cref="Movimento.NumeroDocumentoNoBanco"/>, consultando os
        /// débitos efetuados, depois os agendados e por fim os não efetuados, e para no primeiro segmento que o encontra.
        /// </summary>
        internal override async Task<Movimento> ConsultarComprovantePorNsuAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (Origem == null || Origem.NumeroAgencia <= 0 || Origem.NumeroConta <= 0)
                throw new InvalidOperationException("O Bradesco exige agência e conta da conta pagadora (ConfiguracaoAPI.Correntista).");
            var id = movimento.NumeroDocumentoNoBanco?.Trim();
            if (id == null || !IdTransacaoValido.IsMatch(id))
                throw new InvalidOperationException("Informe em NumeroDocumentoNoBanco o idTransacao do pagamento (devolvido por PagarConvenioAsync).");

            var inicio = movimento.DataPagamento == default(DateTime) ? _relogio().Date.AddDays(-30) : movimento.DataPagamento.Date;
            var fim = (movimento.DataPagamento == default(DateTime) ? _relogio().Date : movimento.DataPagamento.Date).AddDays(15);

            string ultimoErro = null;
            foreach (var segmento in new[]
            {
                (Tipo: 3, Situacao: BancoBrSituacaoEnum.Efetivado),
                (Tipo: 4, Situacao: BancoBrSituacaoEnum.Agendado),
                (Tipo: 5, Situacao: BancoBrSituacaoEnum.Rejeitado),
            })
            {
                var url = $"{_baseUrl}{Origem.NumeroAgencia.ToString(CultureInfo.InvariantCulture)}/{Origem.NumeroConta.ToString(CultureInfo.InvariantCulture)}/1" +
                          $"?tipoConsulta={segmento.Tipo}&segmentoConsulta=99&dataInicial={inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
                          $"&dataFinal={fim.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}&idTransacao={Uri.EscapeDataString(id)}";
                var resposta = await _gateway.SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);

                var retorno = BradescoCampos.Texto(resposta["retorno"]);
                if (retorno != null && retorno != RetornoSucesso)
                {
                    ultimoErro = DescricaoRetorno(retorno, BradescoCampos.Texto(resposta["mensagem"]));
                    continue;
                }

                var registro = Registro(resposta, item);
                if (registro == null) continue;

                AplicarRegistro(movimento, item, registro);
                movimento.Situacao = segmento.Situacao;
                movimento.DetalheRejeicao = segmento.Situacao == BancoBrSituacaoEnum.Rejeitado ? "Débito não efetuado." : null;
                return movimento;
            }

            movimento.Situacao = BancoBrSituacaoEnum.NaoIntegrado;
            movimento.DetalheRejeicao = ultimoErro;
            return movimento;
        }

        internal override Task<IReadOnlyList<Movimento>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de contas de consumo e tributos do Bradesco consulta pelo idTransacao do pagamento: use ConsultarAgendamentoAsync.");

        internal override Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de contas de consumo e tributos do Bradesco não oferece conciliação.");

        internal override Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de contas de consumo e tributos do Bradesco não lista convênios habilitados.");

        #region ::. Mapeamento Movimento <-> Bradesco .::

        private static MovimentoItemPagamentoConvenioCodigoBarra ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            return movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra
                ?? throw new ArgumentException("Informe um MovimentoItemPagamentoConvenioCodigoBarra em Movimento.MovimentoItem.", nameof(movimento));
        }

        private JObject MontarRequisicao(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item, int tipoRegistro, bool exigirId)
        {
            if (Origem == null || Origem.NumeroAgencia <= 0 || Origem.NumeroConta <= 0)
                throw new InvalidOperationException("O Bradesco exige agência e conta da conta pagadora (ConfiguracaoAPI.Correntista).");

            var codigo = CodigoBarras44(item.CodigoBarra);
            var corpo = new JObject
            {
                ["agencia"] = Origem.NumeroAgencia,
                ["digitoAgencia"] = int.TryParse(Origem.DVAgencia, NumberStyles.None, CultureInfo.InvariantCulture, out var digitoAgencia) ? digitoAgencia : 0,
                ["codigoBarras"] = codigo,
                ["conta"] = Origem.NumeroConta,
                ["digitoConta"] = (Origem.DVConta ?? string.Empty).Trim(),
                ["dataDebito"] = DataDebito(movimento).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["tipoRegistro"] = tipoRegistro,
                ["tipoConta"] = 1,
                ["valorPrincipal"] = Valor(movimento, codigo),
            };

            // Na consulta o id é opcional; no pagamento é ele que protege contra duplicidade e permite consultar depois.
            var id = movimento.NumeroDocumento?.Trim();
            if (exigirId || (id != null && IdTransacaoValido.IsMatch(id)))
                corpo["idTransacao"] = IdTransacao(movimento);
            return corpo;
        }

        private static string IdTransacao(Movimento movimento)
        {
            var id = movimento.NumeroDocumento?.Trim();
            if (id == null || !IdTransacaoValido.IsMatch(id))
                throw new ArgumentException("O Bradesco exige em Movimento.NumeroDocumento um identificador único de 1 a 9 caracteres alfanuméricos (idTransacao). Preserve-o nos reenvios.", nameof(movimento));
            return id;
        }

        private static DateTime DataDebito(Movimento movimento) =>
            movimento.DataPagamento == default(DateTime) ? DateTime.Today : movimento.DataPagamento.Date;

        /// <summary>
        /// A API pede o código de barras com 44 dígitos. A representação numérica de 48 dígitos (4 blocos de 11 com
        /// um dígito verificador cada) é convertida descartando esses dígitos.
        /// </summary>
        private static string CodigoBarras44(string texto)
        {
            var digitos = BradescoCampos.Digitos(texto);
            if (digitos.Length == 48)
                digitos = string.Concat(Enumerable.Range(0, 4).Select(bloco => digitos.Substring(bloco * 12, 11)));
            if (digitos.Length != 44)
                throw new InvalidOperationException("Informe o código de barras do convênio com 44 dígitos (ou a representação numérica de 48).");
            return digitos;
        }

        /// <summary>O valor do movimento tem prioridade; sem ele, usa o valor do código quando o código o traz (3º dígito 6 ou 7).</summary>
        private static decimal Valor(Movimento movimento, string codigoBarras)
        {
            if (movimento.ValorPagamento > 0) return movimento.ValorPagamento;
            if ((codigoBarras[2] == '6' || codigoBarras[2] == '7')
                && decimal.TryParse(codigoBarras.Substring(4, 11), NumberStyles.None, CultureInfo.InvariantCulture, out var centavos) && centavos > 0)
                return centavos / 100m;
            throw new InvalidOperationException("Informe o valor do pagamento (Movimento.ValorPagamento): o código de barras não traz o valor.");
        }

        /// <summary>Retorno diferente de 000 é recusa do banco, mesmo com HTTP 200.</summary>
        private static void ValidarRetorno(JObject resposta)
        {
            var retorno = BradescoCampos.Texto(resposta["retorno"]);
            if (retorno != null && retorno != RetornoSucesso)
                throw new BancoApiException(null, new[] { new MensagemErro { Codigo = retorno, Mensagem = DescricaoRetorno(retorno, BradescoCampos.Texto(resposta["mensagem"])) } });
        }

        /// <summary>O registro do documento: o que tem o código de barras pedido; do contrário, o primeiro.</summary>
        private static JObject Registro(JObject resposta, MovimentoItemPagamentoConvenioCodigoBarra item)
        {
            var registros = (resposta["regSaida"] as JArray)?.OfType<JObject>().ToList();
            if (registros == null || registros.Count == 0) return null;

            var codigo = BradescoCampos.Digitos(item.CodigoBarra);
            return registros.FirstOrDefault(r => BradescoCampos.Texto(r["codigoBarras"]) == codigo) ?? registros[0];
        }

        private static void AplicarRegistro(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item, JObject registro)
        {
            if (registro == null) return;

            item.ValorDocumento = ValorCodigoBarras(registro["valorCodigoBarras"]) ?? item.ValorDocumento;
            item.ValorDesconto = BradescoCampos.Decimal(registro["descontos"]) ?? item.ValorDesconto;
            item.ValorJuros = BradescoCampos.Decimal(registro["juros"]) ?? item.ValorJuros;
            item.ValorMulta = BradescoCampos.Decimal(registro["multa"]) ?? item.ValorMulta;
            item.Convenio = BradescoCampos.Texto(registro["nomeEmpresaConveniada"]) ?? item.Convenio;

            var vencimento = DataIso(registro["dataVencimentoOrgao"]);
            if (vencimento.HasValue) item.DataVencimento = vencimento.Value;

            var exercicio = BradescoCampos.Inteiro(registro["exercicio"]);
            if (exercicio.GetValueOrDefault() > 0) item.AnoExercicio = (int)exercicio.Value;

            var autenticacao = BradescoCampos.Inteiro(registro["autenticacaoBancaria"]);
            if (autenticacao.GetValueOrDefault() > 0) item.Autenticacao = autenticacao.Value.ToString(CultureInfo.InvariantCulture);

            var nsu = BradescoCampos.Inteiro(registro["nsuBanco"]);
            if (nsu.GetValueOrDefault() > 0) item.Nsu = nsu;

            var valorDebito = BradescoCampos.Decimal(registro["valorDebito"]);
            if (movimento.ValorPagamento == 0 && valorDebito.GetValueOrDefault() != 0)
                movimento.ValorPagamento = valorDebito.Value;

            var dataPagamento = DataIso(registro["dataPagamento"]);
            var hora = BradescoCampos.Texto(registro["horaPagamento"]);
            if (dataPagamento.HasValue && hora != null && hora.Length == 6 && TimeSpan.TryParseExact(hora, "hhmmss", CultureInfo.InvariantCulture, out var horario))
                movimento.Horario = dataPagamento.Value.Add(horario);
        }

        /// <summary>"Valor original do código de barras": texto com 2 casas decimais implícitas ("0000000006190" = 61,90).</summary>
        private static decimal? ValorCodigoBarras(JToken token)
        {
            var texto = BradescoCampos.Texto(token);
            if (texto == null) return null;
            if (texto.All(char.IsDigit))
                return decimal.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var centavos) ? centavos / 100m : (decimal?)null;
            return BradescoCampos.Decimal(token);
        }

        private static DateTime? DataIso(JToken token) =>
            DateTime.TryParseExact(BradescoCampos.Texto(token), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) && data.Year > 1 ? data : (DateTime?)null;

        private static string DescricaoRetorno(string codigo, string mensagemDoBanco)
        {
            string descricao;
            return Retornos.TryGetValue(codigo.PadLeft(3, '0'), out descricao) ? descricao : mensagemDoBanco ?? "Pagamento não efetuado.";
        }

        /// <summary>Códigos de retorno da transação de negócio (manual de contas de consumo e tributos).</summary>
        private static readonly Dictionary<string, string> Retornos = new Dictionary<string, string>
        {
            { "001", "Empresa não disponível para pagamento" }, { "002", "Empresa não disponível para pagamento" },
            { "003", "Dígito do código de barras inválido" }, { "004", "Opção de processamento inválida" },
            { "006", "Conta já programada para débito" }, { "008", "Data de débito excede data-limite para pagamento" },
            { "009", "Data de débito inválida" }, { "010", "Agência / conta não cadastrada" },
            { "011", "Código de barras inválido" }, { "012", "Data de débito excede data-limite para pagamento" },
            { "013", "Data de débito inválida" }, { "014", "Agência / conta inválida" },
            { "015", "Saldo insuficiente" }, { "016", "Conta / competência bloqueada" },
            { "017", "Tributo não autorizado para pagamento" }, { "018", "Valor inconsistente" },
            { "019", "Conta não autorizada para pagamento" }, { "020", "Data do débito deve ser igual ao dia corrente" },
            { "022", "Documento não autorizado para pagamento" }, { "023", "Data de vencimento inválida" },
            { "024", "Problema no cálculo do valor (moeda diferente de real sem parametrização de conversão)" },
            { "025", "Exercício não autorizado para pagamento" }, { "027", "Anulação inválida" },
            { "029", "Identificador digitado inválido (FGTS)" }, { "030", "Valor de débito digitado inválido" },
            { "031", "Conta com bloqueio judicial" }, { "034", "Data para efetivação do débito inválida (GPS)" },
            { "035", "Código do pagamento inválido (GPS)" }, { "036", "Data de competência inválida (GPS)" },
            { "037", "Código do identificador do contribuinte inválido (GPS)" },
            { "051", "Código de barras inconsistente - Tribunal de Justiça" },
            { "052", "Pagamento via código de barras não autorizado (GPS)" },
            { "053", "Competência 13 tem que ser paga dentro do próprio exercício (GPS)" },
            { "054", "Canal desabilitado para pagamento" }, { "055", "Data do pagamento excede data máxima permitida (GPS)" },
            { "057", "Pagamento duplicado" }, { "058", "Transação recusada com manutenção da sessão" },
            { "059", "Transação recusada com encerramento da sessão" },
        };

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Bradesco.Http;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.ContaCorrente
{
    /// <summary>
    /// API de Saldo e Extrato do Bradesco (<c>v1/fornecimento-extratos-contas</c>), conta corrente PJ.
    /// Devolve o saldo como primeiro item (<see cref="MovimentoItemExtratoSaldo"/>) e depois um item por
    /// lançamento: o saldo anterior vem da linha "Saldo Anterior" do banco e o saldo atual é o saldo após o
    /// último lançamento do período — não há consulta de saldo separada (o endpoint de saldo do Bradesco não é usado).
    ///
    /// <para>O banco separa os lançamentos em blocos: <c>extratoPorPeriodo</c> (históricos, de D-2 em diante) e
    /// <c>extratoUltimosLancamentos</c> (D-1 e D0, só quando a data final é a data atual ou futura). Os lançamentos
    /// futuros (<c>extratoLancamentosFuturos</c>) não entram: ainda não foram contabilizados.</para>
    ///
    /// <para>A conta é a do <c>Correntista</c> (agência e conta sem dígito); <c>numeroContaCorrente</c> é ignorado.
    /// O limite é de 5.000 lançamentos por consulta e a API não tem paginação: se o banco indicar que há mais dados
    /// (<c>codigoRetorno</c> 01), a consulta falha em vez de devolver um extrato incompleto — reduza o período
    /// com <c>diaInicial</c>/<c>diaFinal</c>.</para>
    /// </summary>
    internal sealed class ExtratoClient : ContaCorrenteApiBase
    {
        private const string CodigoSucesso = "00";
        private const string CodigoMaisDados = "01";

        private readonly BradescoHttpGateway _gateway;
        private readonly string _baseUrl;

        internal ExtratoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, Correntista origem, Uri baseUrl = null)
            : base(httpClient, origem)
        {
            _gateway = new BradescoHttpGateway(httpClient, tokenProvider);
            _baseUrl = BradescoHttpGateway.NormalizarBase(baseUrl ?? BradescoHttpGateway.PagamentosBaseUrl);
        }

        internal override async Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default)
        {
            if (agruparCnab) throw new NotSupportedException("O extrato do Bradesco não oferece agrupamento CNAB.");
            if (Origem == null || Origem.NumeroAgencia <= 0 || Origem.NumeroConta <= 0)
                throw new ArgumentException("Informe agência e conta do titular (ConfiguracaoAPI.Correntista) para consultar o extrato do Bradesco.");

            var inicio = new DateTime(ano, mes, diaInicial ?? 1);
            var fim = new DateTime(ano, mes, diaFinal ?? DateTime.DaysInMonth(ano, mes));
            if (fim < inicio) throw new ArgumentException("A data final não pode anteceder a inicial.");

            var url = $"{_baseUrl}v1/fornecimento-extratos-contas/extratos?agencia={Origem.NumeroAgencia.ToString(CultureInfo.InvariantCulture)}" +
                      $"&conta={Origem.NumeroConta.ToString(CultureInfo.InvariantCulture)}&tipo=cc" +
                      $"&dataInicio={inicio.ToString("ddMMyyyy", CultureInfo.InvariantCulture)}&dataFim={fim.ToString("ddMMyyyy", CultureInfo.InvariantCulture)}";
            var resposta = await _gateway.SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);

            decimal? saldoAnterior = null;
            var lancamentos = new List<JObject>();

            // Históricos (D-2 em diante) primeiro, depois D-1 e D0: a ordem cronológica do extrato.
            var periodo = Secao(resposta, "extratoPorPeriodo");
            if (periodo != null)
                Separar(periodo["lstLancamentoMensal"] as JArray, lancamentos, ref saldoAnterior);

            var recentes = Secao(resposta, "extratoUltimosLancamentos");
            var grupos = recentes?["listaLancamentos"] as JObject;
            if (grupos != null)
                foreach (var grupo in new[] { "Saldo Anterior", "Ultimos Lancamentos", "Lancamentos Dia" })
                    Separar(grupos[grupo] as JArray, lancamentos, ref saldoAnterior);

            var transacoes = new List<(MovimentoItemExtratoTransacao Item, decimal Saldo)>();
            foreach (var lancamento in lancamentos)
            {
                var data = BradescoCampos.DataComBarras(lancamento["dataLancamento"]);
                var valor = BradescoCampos.DecimalBr(lancamento["valorLancamento"]);
                var sinal = BradescoCampos.Texto(lancamento["sinalLancamento"]);
                if (!data.HasValue || !valor.HasValue || (sinal != "+" && sinal != "-")) continue;
                if (data.Value < inicio || data.Value > fim) continue;

                var codigo = BradescoCampos.Texto(lancamento["codigoLancamento"]);
                var abreviado = BradescoCampos.Texto(lancamento["descritivoLancamentoAbreviado"]);
                var completo = BradescoCampos.Texto(lancamento["descritivoLancamentoCompleto"]);
                transacoes.Add((new MovimentoItemExtratoTransacao
                {
                    Tipo = sinal == "-" ? BancoBrTipoCreditoDebitoEnum.Debito : BancoBrTipoCreditoDebitoEnum.Credito,
                    Valor = Math.Abs(valor.Value),
                    Data = data.Value,
                    NumeroDocumento = BradescoCampos.Texto(lancamento["numeroDocumento"]),
                    Descricao = completo ?? abreviado,
                    DescricaoInformacaoComplementar = BradescoCampos.Texto(lancamento["segundaLinhalLancamento"]),
                    DescricaoComplementarRetorno = codigo == null && abreviado == null ? null
                        : new List<MovimentoDescricaoComplementarRetorno> { new MovimentoDescricaoComplementarRetorno { Codigo = codigo, Descricao = abreviado } },
                }, SaldoApos(lancamento) ?? 0m));
            }

            var movimentos = new List<Movimento>();
            if (saldoAnterior.HasValue || transacoes.Count > 0)
                movimentos.Add(new Movimento
                {
                    MovimentoItem = new MovimentoItemExtratoSaldo
                    {
                        SaldoAnterior = saldoAnterior.GetValueOrDefault(),
                        SaldoAtual = transacoes.Count > 0 ? transacoes[transacoes.Count - 1].Saldo : saldoAnterior.GetValueOrDefault(),
                    }
                });
            movimentos.AddRange(transacoes.Select(t => new Movimento { MovimentoItem = t.Item }));
            return movimentos;
        }

        /// <summary>
        /// O bloco só existe quando o banco o devolve. Resultado diferente de sucesso, mesmo com HTTP 200, é erro;
        /// "mais dados" não tem como ser paginado e por isso também falha.
        /// </summary>
        private static JObject Secao(JObject resposta, string nome)
        {
            var secao = resposta[nome] as JObject;
            if (secao == null) return null;

            var codigo = BradescoCampos.Texto(secao["codigoRetorno"]);
            var mensagem = BradescoCampos.Texto(secao["mensagem"]);
            if (codigo == CodigoMaisDados)
                throw new InvalidOperationException("O Bradesco indicou que há mais lançamentos do que o limite de 5.000 por consulta (codigoRetorno 01) e a API não pagina: reduza o período da consulta.");
            if (codigo != null && codigo != CodigoSucesso)
                throw new BancoApiException(null, new[] { new MensagemErro { Codigo = codigo, Mensagem = mensagem } });
            return secao;
        }

        /// <summary>Separa a linha "Saldo Anterior" (que dá o saldo do início do período) dos lançamentos.</summary>
        private static void Separar(JArray linhas, List<JObject> lancamentos, ref decimal? saldoAnterior)
        {
            if (linhas == null) return;
            foreach (var linha in linhas.OfType<JObject>())
            {
                if (EhSaldoAnterior(linha))
                    saldoAnterior = saldoAnterior ?? SaldoApos(linha);
                else
                    lancamentos.Add(linha);
            }
        }

        private static bool EhSaldoAnterior(JObject linha) =>
            BradescoCampos.Texto(linha["identificacaoSubCodigo"]) == "S"
            || string.Equals(BradescoCampos.Texto(linha["descritivoLancamentoAbreviado"]), "Saldo Anterior", StringComparison.OrdinalIgnoreCase);

        private static decimal? SaldoApos(JObject linha)
        {
            var saldo = BradescoCampos.DecimalBr(linha["valorSaldoAposLancamento"]);
            return saldo.HasValue ? BradescoCampos.ComSinal(saldo.Value, BradescoCampos.Texto(linha["sinalSaldo"])) : (decimal?)null;
        }
    }
}

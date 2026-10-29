using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.BancoDoBrasil.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.BancoDoBrasil.Extrato
{
    /// <summary>
    /// Consulta o extrato V2 do BB. Retorna os lançamentos contabilizados e o saldo atual;
    /// lançamentos futuros/em processamento não compõem o extrato conciliável.
    /// Agência e conta são obtidas de Correntista, sem dígitos verificadores.
    /// </summary>
    internal class ExtratoClient : ContaCorrenteApiBase
    {
        private readonly BancoDoBrasilHttpGateway _gateway;
        private readonly string _baseUrl;

        internal ExtratoClient(ConfiguracaoAPI configuracao, IAccessTokenProvider tokenProvider)
            : this(BancoDoBrasilHttpGateway.BuildHttpClient(configuracao.CertificadoDigital), tokenProvider,
                  configuracao.ChaveAplicacao, new Uri(configuracao.AmbienteHomologacao
                      ? "https://extratos.mtls.api.hm.bb.com.br/v2/"
                      : "https://extratos.mtls.api.bb.com.br/v2/"), configuracao.Correntista)
        {
        }

        public ExtratoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string chaveAplicacao, Uri baseUrl, Correntista origem)
            : base(httpClient, origem)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));
            _baseUrl = baseUrl.ToString().TrimEnd('/') + "/";
            _gateway = new BancoDoBrasilHttpGateway(httpClient, tokenProvider, chaveAplicacao);
        }

        internal override async Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default)
        {
            if (Origem == null) throw new ArgumentNullException(nameof(Origem));
            if (Origem.NumeroAgencia <= 0 || Origem.NumeroConta <= 0)
                throw new ArgumentException("Informe agência e conta do titular para consultar o extrato BB.");
            var inicio = new DateTime(ano, mes, diaInicial ?? 1);
            var fim = new DateTime(ano, mes, diaFinal ?? DateTime.DaysInMonth(ano, mes));
            if (fim < inicio) throw new ArgumentException("A data final não pode anteceder a inicial.");
            var url = $"{_baseUrl}conta-corrente/agencia/{Origem.NumeroAgencia}/conta/{Origem.NumeroConta}?dataInicioSolicitacao={inicio.ToString("ddMMyyyy", CultureInfo.InvariantCulture)}&dataFimSolicitacao={fim.ToString("ddMMyyyy", CultureInfo.InvariantCulture)}&quantidadeRegistroPaginaSolicitacao=120";
            var movimentos = new List<Movimento>();
            var pagina = 1;
            do
            {
                var resposta = await _gateway.SendAsync<ExtratoResponse>(HttpMethod.Get,
                    $"{url}&numeroPaginaSolicitacao={pagina}", null, cancellationToken).ConfigureAwait(false);
                if (resposta == null) throw new JsonSerializationException("Resposta vazia ao consultar extrato BB.");
                foreach (var item in resposta.Lancamentos ?? new List<LancamentoResponse>())
                {
                    // A API também inclui posições de saldo e operações ainda não contabilizadas.
                    if (item.TipoLancamento == "SA")
                    {
                        movimentos.Add(new Movimento { MovimentoItem = new MovimentoItemExtratoSaldo { SaldoAtual = ValorComSinal(item) } });
                    }
                    else if (item.TipoLancamento == "1")
                    {
                        movimentos.Add(new Movimento
                        {
                            MovimentoItem = new MovimentoItemExtratoTransacao
                            {
                                TransactionId = item.IdentificadorUnico,
                                NumeroDocumento = item.NumeroDocumento.ToString(CultureInfo.InvariantCulture),
                                Tipo = Tipo(item.Sinal), Valor = Math.Abs(item.Valor),
                                Data = Data(item.DataLancamento),
                                DataLote = item.DataMovimento == 0 ? (DateTime?)null : Data(item.DataMovimento),
                                Descricao = item.Descricao, DescricaoInformacaoComplementar = item.Complemento,
                                DescricaoComplementarRetorno = DescricoesComplementares(item),
                                CpfCnpj = item.CpfCnpjContrapartida
                            }
                        });
                    }
                }
                if (resposta.ProximaPagina == 0) break;
                if (resposta.ProximaPagina <= pagina)
                    throw new JsonSerializationException("Paginação não progressiva no extrato BB.");
                pagina = resposta.ProximaPagina;
            } while (true);
            return movimentos;
        }

        /// <summary>
        /// Preserva histórico e sub-histórico em pares separados. A OpenAPI v2 não declara
        /// textoDescricaoHistorico; a descrição do histórico só é preenchida se vier na resposta.
        /// </summary>
        private static List<MovimentoDescricaoComplementarRetorno> DescricoesComplementares(LancamentoResponse item)
        {
            var descricoes = new List<MovimentoDescricaoComplementarRetorno>();
            if (item.CodigoHistorico.HasValue || !string.IsNullOrEmpty(item.DescricaoHistorico))
                descricoes.Add(new MovimentoDescricaoComplementarRetorno
                {
                    Codigo = item.CodigoHistorico?.ToString(CultureInfo.InvariantCulture),
                    Descricao = item.DescricaoHistorico
                });
            if (item.CodigoSubHistorico.HasValue || !string.IsNullOrEmpty(item.Descricao))
                descricoes.Add(new MovimentoDescricaoComplementarRetorno
                {
                    Codigo = item.CodigoSubHistorico?.ToString(CultureInfo.InvariantCulture),
                    Descricao = item.Descricao
                });
            return descricoes;
        }

        private static DateTime Data(int valor) => DateTime.ParseExact(valor.ToString("D8", CultureInfo.InvariantCulture), "ddMMyyyy", CultureInfo.InvariantCulture);

        private static BancoBrTipoCreditoDebitoEnum Tipo(string sinal)
        {
            switch (sinal)
            {
                case "C": return BancoBrTipoCreditoDebitoEnum.Credito;
                case "D": return BancoBrTipoCreditoDebitoEnum.Debito;
                default: throw new JsonSerializationException($"Sinal não conciliável no extrato BB: {sinal}.");
            }
        }

        private static decimal ValorComSinal(LancamentoResponse item) =>
            Tipo(item.Sinal) == BancoBrTipoCreditoDebitoEnum.Debito ? -Math.Abs(item.Valor) : Math.Abs(item.Valor);
    }

    internal class ExtratoResponse
    {
        [JsonProperty("numeroPaginaProximo")] public int ProximaPagina { get; set; }
        [JsonProperty("listaLancamento")] public List<LancamentoResponse> Lancamentos { get; set; }
    }

    internal class LancamentoResponse
    {
        [JsonProperty("indicadorTipoLancamento")] public string TipoLancamento { get; set; }
        [JsonProperty("dataLancamento")] public int DataLancamento { get; set; }
        [JsonProperty("dataMovimento")] public int DataMovimento { get; set; }
        [JsonProperty("numeroDocumento")] public long NumeroDocumento { get; set; }
        [JsonProperty("codigoHistorico")] public int? CodigoHistorico { get; set; }
        [JsonProperty("textoDescricaoHistorico")] public string DescricaoHistorico { get; set; }
        [JsonProperty("codigoSubHistorico")] public int? CodigoSubHistorico { get; set; }
        [JsonProperty("textoDescricaoSubHistorico")] public string Descricao { get; set; }
        [JsonProperty("valorLancamento")] public decimal Valor { get; set; }
        [JsonProperty("indicadorSinalLancamento")] public string Sinal { get; set; }
        [JsonProperty("textoInformacaoComplementar")] public string Complemento { get; set; }
        [JsonProperty("numeroCadastroPessoaFisicaCadastroNacPessoasJuridicasContrapartida")] public string CpfCnpjContrapartida { get; set; }
        [JsonProperty("textoIdentificadorUnicoTransacao")] public string IdentificadorUnico { get; set; }
    }
}

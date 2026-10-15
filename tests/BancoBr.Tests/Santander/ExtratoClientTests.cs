using System.Net;
using System.Text;
using BancoBr.API.Santander.ContaCorrente;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.Santander
{
    public class ExtratoClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/bank_account_information/v1");

        private static ExtratoClient Criar(HttpMessageHandler handler) =>
            new ExtratoClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem());

        private static Correntista CriarOrigem() => new Correntista { NumeroAgencia = 1, NumeroConta = 1033160, DVConta = "7" };

        private static string Pagina(int quantidade)
        {
            var itens = Enumerable.Range(0, quantidade).Select(i =>
                @"{ ""creditDebitType"": ""DEBITO"", ""transactionName"": ""Lancamento " + i + @""", ""amount"": 10.50, ""transactionDate"": ""2026-09-05"" }");
            return @"{ ""_content"": [" + string.Join(",", itens) + "] }";
        }

        [Fact]
        public async Task ConsultarExtratoAsync_MontaUrlMapeiaLancamentos()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{ ""_content"": [
                { ""transactionId"": ""tx-001"", ""creditDebitType"": ""CREDITO"", ""transactionName"": ""PIX RECEBIDO"", ""amount"": 150.25, ""transactionDate"": ""2026-09-03"" },
                { ""creditDebitType"": ""DEBITO"", ""transactionName"": ""TARIFA"", ""amount"": -7.90, ""transactionDate"": ""2026-09-04"" }
            ] }"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false, default);

            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal($"{BaseUrl}/banks/0033/statements/0001.000010331607?initialDate=2026-09-01&finalDate=2026-09-30&_limit=50&_offset=0", request.RequestUri!.ToString());
            Assert.Equal("fake-client-id", request.Headers.GetValues("X-Application-Key").Single());

            Assert.Equal(2, resultado.Count);
            var credito = (MovimentoItemExtratoTransacao)resultado[0].MovimentoItem;
            Assert.Equal("tx-001", credito.TransactionId);
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Credito, credito.Tipo);
            Assert.Equal(150.25m, credito.Valor);
            Assert.Equal(new DateTime(2026, 9, 3), credito.Data);
            Assert.Equal("PIX RECEBIDO", credito.Descricao);

            var debito = (MovimentoItemExtratoTransacao)resultado[1].MovimentoItem;
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Debito, debito.Tipo);
            Assert.Equal(7.90m, debito.Valor);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_RecortaPeriodoEPagina()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, Pagina(50)), (HttpStatusCode.OK, Pagina(3)));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 2, 2026, 5, 10, false, default);

            Assert.Equal(53, resultado.Count);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains("initialDate=2026-02-05&finalDate=2026-02-10&_limit=50&_offset=0", handler.Requests[0].RequestUri!.Query.Replace("?", ""));
            Assert.EndsWith("_limit=50&_offset=50", handler.Requests[1].RequestUri!.Query);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_SemOrigem_Lanca()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => new ExtratoClient(new HttpClient(new SequencedFakeHttpMessageHandler()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, null!).ConsultarExtratoAsync(0, 9, 2026, null, null, false));
        }
    }
}

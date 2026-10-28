using BancoBr.API.Core.Operacoes;
using System.Net;
using BancoBr.API.Sicredi;
using BancoBr.API.Core.Errors;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BancoBr.Tests.Sicredi
{
    public class SicrediClientTests
    {
        private static SicrediHttpGateway Gateway(HttpMessageHandler handler) => new SicrediHttpGateway(new HttpClient(handler), new FakeOAuthTokenProvider(),
            new Correntista { NumeroAgencia = 100, NumeroConta = 0, DVConta = "1", CPF_CNPJ = "11.111.111/0001-11" }, true);
        private static Movimento Boleto() => new Movimento { NumeroDocumento = "teste", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 10,
            MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = "74892950500006000001123100010108100200003107" } };

        [Theory]
        [InlineData("RECEBIDO", BancoBrSituacaoEnum.Agendado)]
        [InlineData("AGENDADO", BancoBrSituacaoEnum.Agendado)]
        [InlineData("AGUARDANDO_APROVACAO", BancoBrSituacaoEnum.Agendado)]
        [InlineData("APROVADO", BancoBrSituacaoEnum.Agendado)]
        [InlineData("SUCESSO", BancoBrSituacaoEnum.Efetivado)]
        [InlineData("CANCELADO", BancoBrSituacaoEnum.Cancelado)]
        [InlineData("ERRO", BancoBrSituacaoEnum.Rejeitado)]
        [InlineData("NAO_APROVADO", BancoBrSituacaoEnum.Rejeitado)]
        [InlineData("EXPIRADO", BancoBrSituacaoEnum.Rejeitado)]
        [InlineData("NOVO", BancoBrSituacaoEnum.NaoIntegrado)]
        public async Task Pagar_Boleto_UsaContratoEEstadoAssincrono(string status, BancoBrSituacaoEnum esperado)
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"" + status + "\"}"));
            var m = Boleto();
            await new PagamentosClient(Gateway(handler)).PagarAsync(m, "transacao-1", default);
            var body = JObject.Parse(handler.Bodies[0]!);
            Assert.Equal("000001", (string?)body["conta"]);
            Assert.Equal("0100", (string?)body["cooperativa"]);
            Assert.Equal("11111111000111", (string?)body["documento"]);
            Assert.Equal("2026-10-28", (string?)body["dataPagamento"]);
            Assert.Equal(10m, (decimal)body["valorPagamento"]!);
            Assert.Equal("transacao-1", (string?)body["idTransacao"]);
            Assert.Equal("/sb/multipag-pagamento-sandbox/v1/pagamentos/boletos", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Equal(esperado, m.SituacaoBancoBr);
            Assert.Equal(status, MetadadosOperacao.De(m).SituacaoOriginal);
            Assert.Equal("transacao-1", m.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task Reenvio_ErroServidor_PreservaIdentidade()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.InternalServerError, "{}"), (HttpStatusCode.OK, "{\"status\":\"RECEBIDO\"}"));
            var api = new PagamentosClient(Gateway(handler)); var m = Boleto();
            await Assert.ThrowsAsync<BancoApiException>(() => api.PagarAsync(m, "fixo", default));
            await api.PagarAsync(m, "outro", default);
            Assert.Equal(JObject.Parse(handler.Bodies[0]!), JObject.Parse(handler.Bodies[1]!));
        }

        [Fact]
        public async Task Cancelar_Tributo_UsaPatchComContaEId()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, ""));
            var m = new Movimento { NumeroDocumentoNoBanco = "original", MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra() };
            await new PagamentosClient(Gateway(handler)).CancelarAsync(m, default);
            Assert.Equal("PATCH", handler.Requests[0].Method.Method);
            Assert.EndsWith("tributos/barras/cancelamentos", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("original", (string?)JObject.Parse(handler.Bodies[0]!)["idTransacao"]);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, m.SituacaoBancoBr);
        }

        [Fact]
        public async Task Consultar_401_RenovaTokenENormalizaErro()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.Unauthorized, "{}"), (HttpStatusCode.Forbidden, "{\"detail\":\"Privilégios insuficientes\"}"));
            var m = Boleto(); m.NumeroDocumentoNoBanco = "id/um";
            var e = await Assert.ThrowsAsync<BancoApiException>(() => new PagamentosClient(Gateway(handler)).ConsultarAsync(m, default));
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(403, e.HttpStatusCode);
            Assert.Contains("Privilégios insuficientes", e.Message);
            Assert.Equal("0100", handler.Requests[1].Headers.GetValues("x-cooperativa").Single());
            Assert.Contains("id%2Fum", handler.Requests[1].RequestUri!.AbsoluteUri);
        }

        [Fact]
        public async Task Extrato_PaginaEMapeiaSaldoEDebito()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"saldoAnterior\":100,\"pageNumber\":0,\"totalPages\":2,\"dtlMovimentos\":[{\"data\":\"2026-10-01\",\"valor\":-10,\"saldo\":90,\"idMovimento\":\"um\"}]}"),
                (HttpStatusCode.OK, "{\"pageNumber\":1,\"totalPages\":2,\"dtlMovimentos\":[{\"data\":\"2026-10-02\",\"valor\":20,\"saldo\":110}]}"));
            var r = await new ExtratoClient(Gateway(handler)).ConsultarExtratoAsync(1, 10, 2026, null, null, false);
            Assert.Equal(3, r.Count);
            Assert.Contains("page=1", handler.Requests[1].RequestUri!.Query);
            var saldo = Assert.IsType<MovimentoItemExtratoSaldo>(r[0].MovimentoItem);
            Assert.Equal(100, saldo.SaldoAnterior); Assert.Equal(110, saldo.SaldoAtual);
            var debito = Assert.IsType<MovimentoItemExtratoTransacao>(r[1].MovimentoItem);
            Assert.Equal(10, debito.Valor); Assert.Equal(BancoBrTipoCreditoDebitoEnum.Debito, debito.Tipo);
            Assert.Equal(90, MetadadosOperacao.De(debito).SaldoAposLancamento);
        }

        [Fact]
        public async Task Extrato_PeriodoInvertido_NaoEnvia()
        {
            var handler = new SequencedFakeHttpMessageHandler();
            await Assert.ThrowsAsync<ArgumentException>(() => new ExtratoClient(Gateway(handler)).ConsultarExtratoAsync(1, 10, 2026, 20, 1, false));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Saldo_UsaRotaSemParametros()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"saldoAtual\":400,\"saldoBloqueioJudicial\":10,\"limiteChequeEspecialDisponivel\":200}"));
            var m = await new ExtratoClient(Gateway(handler)).ConsultarSaldoAsync(default);
            var saldo = Assert.IsType<MovimentoItemExtratoSaldo>(m.MovimentoItem);
            Assert.Equal(400, saldo.SaldoAtual); Assert.Equal(10, saldo.SaldoBloqueioJudicial);
            Assert.Equal("/sb/contacorrente/v1/saldos", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Empty(handler.Requests[0].RequestUri!.Query);
        }

        [Fact]
        public async Task Fachada_AutenticaComEscopoEEnviaDiretamenteComIdentidadeEstavel()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"access_token\":\"ficticio\",\"expires_in\":3600}"),
                (HttpStatusCode.OK, "{\"status\":\"RECEBIDO\"}"),
                (HttpStatusCode.OK, "{\"status\":\"SUCESSO\"}"));
            var banco = BancoBr.API.BancoApi.ConectarSicredi(new BancoBr.API.Base.Models.ConfiguracaoAPI
            {
                AmbienteHomologacao = true, ClientId = "id-ficticio", ClientSecret = "segredo-ficticio",
                Correntista = new Correntista { NumeroAgencia = 100, NumeroConta = 0, DVConta = "1", CPF_CNPJ = "11111111000111" }
            }, null, null, new HttpClient(handler));
            var m = Boleto();
            await banco.EnviarAgendamentoAsync(m, "identidade-erp");
            await banco.ConsultarAgendamentoAsync(m);
            Assert.Equal("/sb/thirdparty/auth/token", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Contains("grant_type=client_credentials", handler.Bodies[0]);
            Assert.Contains("client_secret=segredo-ficticio", handler.Bodies[0]);
            Assert.DoesNotContain("dda", handler.Bodies[0]);
            Assert.Equal("identidade-erp", (string?)JObject.Parse(handler.Bodies[1]!)["idTransacao"]);
            Assert.EndsWith("/boletos/identidade-erp", handler.Requests[2].RequestUri!.AbsolutePath);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, m.SituacaoBancoBr);
        }

        [Fact]
        public async Task Pagar_Tributo_EnviaFgtsSemConsultaPrevia()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"AGENDADO\"}"));
            var m = Boleto(); m.MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra
            { CodigoBarra = "81610000001196248692023101183215529722698854", IdentificadorFgts = "0181" };
            await new PagamentosClient(Gateway(handler)).PagarAsync(m, "tributo", default);
            Assert.EndsWith("/tributos/barras", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("0181", (string?)JObject.Parse(handler.Bodies[0]!)["idFgts"]);
        }

        [Fact]
        public async Task Pagar_PixPorChave_EnviaDocumentoBeneficiario()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"RECEBIDO\"}"));
            var m = Boleto(); m.MovimentoItem = new MovimentoItemTransferenciaPIX { ChavePIX = "TESTE@EXAMPLE.COM" };
            m.Favorecido = new Favorecido { CPF_CNPJ = "111.111.111-11", Nome = "Teste" };
            await new PagamentosClient(Gateway(handler)).PagarAsync(m, "pix", default);
            var b = JObject.Parse(handler.Bodies[0]!);
            Assert.EndsWith("/pix/chave", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("teste@example.com", (string?)b["chavePix"]);
            Assert.Equal("11111111111", (string?)b["documentoBeneficiario"]);
        }

        [Fact]
        public async Task Dda_UsaBasePropriaEMapeiaBoleto()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"boletos\":[{\"nomeBeneficiario\":\"Teste\",\"dataVencimento\":\"2026-10-28\",\"valorTitulo\":10,\"codigoBarras\":\"748\"}]}"));
            var r = await new DdaClient(Gateway(handler)).ConsultarAsync(new DateTime(2026, 10, 28), new DateTime(2026, 10, 29),
                BancoBr.API.Base.Models.SituacaoBoletoEnum.EmAberto, BancoBr.API.Base.Models.TipoDataConsultaEnum.Vencimento, default);
            Assert.Equal("/sb/multipag-sandbox/v1/dda/boletos", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Contains("cnpjCpfPagador=11111111000111", handler.Requests[0].RequestUri!.Query);
            Assert.Equal(10, Assert.IsType<MovimentoItemDDA>(Assert.Single(r).MovimentoItem).ValorBoleto);
        }

        private sealed class PdfHandler : HttpMessageHandler
        {
            internal readonly byte[] Bytes = { 37, 80, 68, 70, 45, 255, 0, 128 };
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Assert.Equal("application/pdf", request.Headers.Accept.Single().MediaType);
                Assert.EndsWith("/boletos/id/comprovantes", request.RequestUri!.AbsolutePath);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Bytes) });
            }
        }

        [Fact]
        public async Task Comprovante_PreservaBytesBinarios()
        {
            var handler = new PdfHandler(); var m = Boleto(); m.NumeroDocumentoNoBanco = "id";
            var bytes = await new PagamentosClient(Gateway(handler)).ComprovanteAsync(m, default);
            Assert.Equal(handler.Bytes, bytes);
        }
    }
}

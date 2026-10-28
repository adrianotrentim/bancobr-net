using System.Net;
using System.Net.Http;
using BancoBr.API.BancoDoBrasil.Pagamentos;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BancoBr.Tests.BancoDoBrasil
{
    public class PagamentosClientTests
    {
        private static Correntista Origem() => new Correntista { NumeroAgencia = 1234, NumeroConta = 56789, DVConta = "X" };
        private static Movimento Boleto() => new Movimento {
            NumeroDocumento = "1234567", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 40.75m,
            Favorecido = new Favorecido { CPF_CNPJ = "12.345.678/0001-00", TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ },
            MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = new string('1', 44) }
        };

        [Fact]
        public async Task PagarBoleto_Aceite_NaoConfundeComLiquidacaoEPreservaRequisicao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, "{\"estadoRequisicao\":1,\"lancamentos\":[{\"codigoIdentificadorPagamento\":90001210123456001,\"indicadorAceite\":\"S\",\"errorCodes\":[0]}]}");
            var client = new PagamentoBoletoClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem(), 123);
            var movimento = Boleto();
            await client.PagarBoletoComConsultaAsync(movimento, Guid.NewGuid());
            var request = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal(1234567, (int)request["numeroRequisicao"]!);
            Assert.Equal(1234, (int)request["numeroAgenciaDebito"]!);
            Assert.Equal("X", (string)request["digitoVerificadorContaCorrenteDebito"]!);
            Assert.Equal("12345678000100", (string)request["lancamentos"]![0]!["documentoBeneficiario"]!);
            Assert.Equal(28102026, (int)request["lancamentos"]![0]!["dataPagamento"]!);
            Assert.Contains("/v1/lotes-boletos?gw-dev-app-key=chave", handler.LastRequest!.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
            Assert.Equal("90001210123456001", movimento.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, movimento.SituacaoBancoBr);
        }

        [Fact]
        public async Task PagarBoleto_NumeroDocumentoInvalido_NaoEnvia()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Created);
            var client = new PagamentoBoletoClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var movimento = Boleto(); movimento.NumeroDocumento = Guid.NewGuid().ToString();
            await Assert.ThrowsAsync<ArgumentException>(() => client.PagarBoletoAsync(movimento, "qualquer"));
            Assert.Null(handler.LastRequest);
        }

        [Theory]
        [InlineData("PAGO", BancoBrSituacaoEnum.Efetivado)]
        [InlineData("AGENDADO", BancoBrSituacaoEnum.Agendado)]
        [InlineData("CONSISTENTE", BancoBrSituacaoEnum.NaoIntegrado)]
        [InlineData("REJEITADO", BancoBrSituacaoEnum.Rejeitado)]
        public async Task ConsultarTed_MapeiaEstadoDoLancamento(string estado, BancoBrSituacaoEnum esperado)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"estadoPagamento\":\"" + estado + "\",\"codigoAutenticacaoPagamento\":\"ABC\",\"valorPagamento\":50}");
            var client = new PagamentoTedClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var movimento = new Movimento { NumeroDocumentoNoBanco = "90001210123456001", MovimentoItem = new MovimentoItemTransferenciaTED() };
            await client.ConsultarTedAsync(movimento);
            Assert.Equal(esperado, movimento.SituacaoBancoBr);
            Assert.Equal("ABC", ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).CodigoAutenticacao);
        }

        [Fact]
        public async Task PagarPix_Email_EnviaCamposOficiaisSemResolverDict()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, "{\"estadoRequisicao\":1,\"listaTransferencias\":[{\"identificadorPagamento\":123,\"indicadorMovimentoAceito\":\"S\"}]}");
            var client = new PagamentoPixClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            await client.PagarComIniciacaoAsync(new Movimento { NumeroDocumento = "123", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 20, MovimentoItem = new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "teste@example.com" } });
            var request = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal(2, (int)request["listaTransferencias"]![0]!["formaIdentificacao"]!);
            Assert.Equal("teste@example.com", (string)request["listaTransferencias"]![0]!["email"]!);
        }

        [Fact]
        public async Task PagarConvenio_ErroNegocial_PreservaCodigoMensagem()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity, "{\"errors\":[{\"codigo\":\"123\",\"mensagem\":\"Lote inválido\"}]}");
            var client = new PagamentoConvenioClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var movimento = Boleto(); movimento.MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra { CodigoBarra = new string('8', 44) };
            var ex = await Assert.ThrowsAsync<BancoApiException>(() => client.PagarConvenioAsync(movimento));
            Assert.Equal("123", ex.Mensagens[0].Codigo);
            Assert.Equal("Lote inválido", ex.Mensagens[0].Mensagem);
        }
        [Fact]
        public async Task PagarTed_MapeiaContaBeneficiarioEFinalidade()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"estadoRequisicao\":1,\"transferencias\":[{\"identificadorTransferencia\":987,\"indicadorMovimentoAceito\":\"S\"}]}");
            var client = new PagamentoTedClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem(), 123);
            var movimento = Boleto();
            movimento.MovimentoItem = new MovimentoItemTransferenciaTED { Banco = 341, NumeroAgencia = 4321, NumeroConta = 98765, DVConta = "1", CodigoFinalidadeTED = FinalidadeTEDEnum.CreditoEmConta };
            await client.PagarTedAsync(movimento, "ignorada");
            var request = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal(123, (int)request["numeroContratoPagamento"]!);
            Assert.Equal(341, (int)request["listaTransferencias"]![0]!["numeroCOMPE"]!);
            Assert.Equal(10, (int)request["listaTransferencias"]![0]!["codigoFinalidadeTED"]!);
            Assert.Equal("987", movimento.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task CancelarBoleto_Confirmado_AplicaCancelado()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"pagamentos\":[{\"codigoPagamento\":987,\"indicadorCancelamento\":\"S\",\"estadoPagamento\":\"CANCELADO\"}]}");
            var client = new PagamentoBoletoClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var movimento = Boleto(); movimento.NumeroDocumentoNoBanco = "987";
            await client.CancelarAgendamentoAsync(movimento);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, movimento.SituacaoBancoBr);
            Assert.Equal(987, (int)JObject.Parse(handler.LastRequestBody!)["listaPagamentos"]![0]!["codigoPagamento"]!);
        }

        [Fact]
        public async Task PagarConvenio_LancamentoInconsistente_AplicaRejeicao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, "{\"codigoEstado\":3,\"lancamentos\":[{\"indicadorAceite\":\"N\",\"errors\":[248]}]}");
            var client = new PagamentoConvenioClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var movimento = Boleto(); movimento.MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra { CodigoBarra = new string('8', 44) };
            await client.PagarConvenioAsync(movimento);
            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, movimento.SituacaoBancoBr);
            Assert.Equal("248", movimento.DetalheRejeicaoBancoBr);
        }
    }
}

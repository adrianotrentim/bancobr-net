using System.Net;
using System.Reflection;
using BancoBr.API;
using BancoBr.API.Base;
using BancoBr.API.BancoDoBrasil.Pagamentos;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json.Linq;
using Xunit;
using ExtratoClient = BancoBr.API.BancoDoBrasil.Extrato.ExtratoClient;

namespace BancoBr.Tests.BancoDoBrasil
{
    public class BancoApiTests
    {
        private static Correntista Origem() => new Correntista
        {
            Banco = (int)BancoEnum.BancoDoBrasil, NumeroAgencia = 1234, NumeroConta = 56789, DVConta = "X"
        };

        private static BancoApi Fachada(PagamentoBoletoApiBase? boleto = null, PagamentoConvenioApiBase? convenio = null,
            ContaCorrenteApiBase? extrato = null)
        {
            var construtor = typeof(BancoApi).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (BancoApi)construtor.Invoke(new object?[] { Origem(), boleto, convenio, null, null, extrato, null, null, null, null });
        }

        private static Movimento MovimentoConvenio() => new Movimento
        {
            NumeroDocumento = "1234567", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 40.75m,
            MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra { CodigoBarra = new string('8', 44) }
        };

        [Fact]
        public async Task EnviarAgendamentoAsync_ConvenioBB_EnviaSemConsultaPrevia()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Created,
                "{\"estadoRequisicao\":1,\"lancamentos\":[{\"codigoIdentificadorPagamento\":90001210123456001,\"indicadorAceite\":\"S\"}]}");
            var convenio = new PagamentoConvenioClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var banco = Fachada(convenio: convenio);
            var movimento = MovimentoConvenio();

            var resultado = await banco.EnviarAgendamentoAsync(movimento);

            Assert.Same(movimento, resultado);
            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Contains("/lotes-guias-codigo-barras?", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal(1234567, (int)JObject.Parse(handler.LastRequestBody!)["numeroRequisicao"]!);
            Assert.Equal("90001210123456001", movimento.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, movimento.Situacao);
        }

        [Fact]
        public async Task ConsultarAgendamentoAsync_ConvenioBBEnviado_ConsultaIdentificadorDoBanco()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"estadoPagamento\":\"PAGO\",\"codigoAutenticacaoPagamento\":\"ABC\"}");
            var convenio = new PagamentoConvenioClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());
            var movimento = MovimentoConvenio();
            movimento.NumeroDocumentoNoBanco = "90001210123456001";

            await Fachada(convenio: convenio).ConsultarAgendamentoAsync(movimento);

            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Contains("/guias-codigo-barras/90001210123456001?", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
            Assert.Equal("ABC", ((MovimentoItemPagamentoConvenioCodigoBarra)movimento.MovimentoItem).Autenticacao);
        }

        [Fact]
        public async Task ConsultarAgendamentoAsync_ConvenioBBSemEnvio_RecusaConsultaPreviaSemHttp()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
            var convenio = new PagamentoConvenioClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave", Origem());

            await Assert.ThrowsAsync<NotSupportedException>(() => Fachada(convenio: convenio).ConsultarAgendamentoAsync(MovimentoConvenio()));

            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_BB_UsaContaDaConfiguracao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"numeroPaginaProximo\":0,\"listaLancamento\":[{\"indicadorTipoLancamento\":\"1\",\"dataLancamento\":28102026,\"indicadorSinalLancamento\":\"D\",\"valorLancamento\":25,\"textoDescricaoSubHistorico\":\"Pagamento\"}]}");
            var extrato = new ExtratoClient(new HttpClient(handler), new StaticAccessTokenProvider("token"), "chave",
                new Uri("https://extratos.example.test/v2/"), Origem());

            var movimentos = await Fachada(extrato: extrato).ConsultarExtratoAsync(99999, 10, 2026, 28, 28);

            Assert.Contains("/agencia/1234/conta/56789?", handler.LastRequest!.RequestUri!.ToString());
            var item = Assert.IsType<MovimentoItemExtratoTransacao>(Assert.Single(movimentos).MovimentoItem);
            Assert.Equal(25m, item.Valor);
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Debito, item.Tipo);
        }
    }
}

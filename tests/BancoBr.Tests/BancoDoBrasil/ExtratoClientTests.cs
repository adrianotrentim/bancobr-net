using System.Net;
using BancoBr.API.BancoDoBrasil.Extrato;
using BancoBr.API.Core.Errors;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json;
using Xunit;

namespace BancoBr.Tests.BancoDoBrasil
{
    public class ExtratoClientTests
    {
        private static ExtratoClient Criar(SequencedFakeHttpMessageHandler handler, Correntista? origem = null) =>
            new ExtratoClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "chave-ficticia", new Uri("https://extratos.mtls.api.hm.bb.com.br/v2"),
                origem ?? new Correntista { NumeroAgencia = 297, NumeroConta = 1213, DVConta = "X" });

        [Fact]
        public async Task ConsultarExtratoAsync_UsaOrigemEPaginaPeloIndicadorOficial()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{
                ""numeroPaginaProximo"":2,""listaLancamento"": [
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":1092026,""numeroDocumento"":123,""textoIdentificadorUnicoTransacao"":""tx1"",""textoDescricaoSubHistorico"":""Pix recebido"",""valorLancamento"":150.25,""indicadorSinalLancamento"":""C"",""numeroCadastroPessoaFisicaCadastroNacPessoasJuridicasContrapartida"":""AB123456000190""},
                    {""indicadorTipoLancamento"":""2"",""indicadorSinalLancamento"":""D"",""valorLancamento"":20},
                    {""indicadorTipoLancamento"":""SA"",""indicadorSinalLancamento"":""D"",""valorLancamento"":50}
                ]}"), (HttpStatusCode.OK, @"{""numeroPaginaProximo"":0,""listaLancamento"":[
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":2092026,""dataMovimento"":1092026,""valorLancamento"":-7.90,""indicadorSinalLancamento"":""D""}
                ]}"));
            var resultado = await Criar(handler).ConsultarExtratoAsync(999, 9, 2026, 1, 10, false);
            Assert.Equal(2, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Contains("/conta-corrente/agencia/297/conta/1213?", request.RequestUri!.ToString());
            Assert.Contains("dataInicioSolicitacao=01092026&dataFimSolicitacao=10092026", request.RequestUri.Query);
            Assert.Contains("quantidadeRegistroPaginaSolicitacao=120", request.RequestUri.Query);
            Assert.Contains("gw-dev-app-key=chave-ficticia", request.RequestUri.Query);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            Assert.Contains("numeroPaginaSolicitacao=2", handler.Requests[1].RequestUri!.Query);
            Assert.Equal(3, resultado.Count);
            var credito = Assert.IsType<MovimentoItemExtratoTransacao>(resultado[0].MovimentoItem);
            Assert.Equal("tx1", credito.TransactionId);
            Assert.Equal("123", credito.NumeroDocumento);
            Assert.Equal("AB123456000190", credito.CpfCnpj);
            Assert.Equal("Pix recebido", credito.Descricao);
            Assert.Equal(150.25m, credito.Valor);
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Credito, credito.Tipo);
            Assert.Equal(new DateTime(2026, 9, 1), credito.Data);
            Assert.Equal(-50m, Assert.IsType<MovimentoItemExtratoSaldo>(resultado[1].MovimentoItem).SaldoAtual);
            var debito = Assert.IsType<MovimentoItemExtratoTransacao>(resultado[2].MovimentoItem);
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Debito, debito.Tipo);
            Assert.Equal(7.90m, debito.Valor);
            Assert.Equal(new DateTime(2026, 9, 1), debito.DataLote);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_ErroNegocial_NormalizaCodigoEMensagem()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.UnprocessableEntity, @"{""code"":""123"",""message"":""Conta inválida""}"));
            var erro = await Assert.ThrowsAsync<BancoApiException>(() => Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false));
            Assert.Equal("123", Assert.Single(erro.Mensagens).Codigo);
            Assert.Contains("Conta inválida", erro.Message);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_PaginacaoCircular_Falha()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""numeroPaginaProximo"":1,""listaLancamento"":[]}"));
            await Assert.ThrowsAsync<JsonSerializationException>(() => Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false));
        }

        [Fact]
        public async Task ConsultarExtratoAsync_SinalBloqueado_NaoPresumeCredito()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[{""indicadorTipoLancamento"":""1"",""indicadorSinalLancamento"":""*""}]}"));
            await Assert.ThrowsAsync<JsonSerializationException>(() => Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false));
        }

        [Fact]
        public async Task ConsultarExtratoAsync_PeriodoInvertido_NaoEnviaRequisicao()
        {
            var handler = new SequencedFakeHttpMessageHandler();
            await Assert.ThrowsAsync<ArgumentException>(() => Criar(handler).ConsultarExtratoAsync(0, 9, 2026, 10, 1, false));
            Assert.Empty(handler.Requests);
        }
    }
}

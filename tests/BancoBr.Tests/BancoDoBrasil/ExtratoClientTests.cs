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
            Assert.Equal(-50m, Assert.IsType<MovimentoItemExtratoSaldo>(resultado[0].MovimentoItem).SaldoAtual);
            var credito = Assert.IsType<MovimentoItemExtratoTransacao>(resultado[1].MovimentoItem);
            Assert.Equal("tx1", credito.TransactionId);
            Assert.Equal("123", credito.NumeroDocumento);
            Assert.Equal("AB123456000190", credito.CpfCnpj);
            Assert.Equal("Pix recebido", credito.Descricao);
            Assert.Equal(150.25m, credito.Valor);
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Credito, credito.Tipo);
            Assert.Equal(new DateTime(2026, 9, 1), credito.Data);
            var debito = Assert.IsType<MovimentoItemExtratoTransacao>(resultado[2].MovimentoItem);
            Assert.Equal(BancoBrTipoCreditoDebitoEnum.Debito, debito.Tipo);
            Assert.Equal(7.90m, debito.Valor);
            Assert.Equal(new DateTime(2026, 9, 1), debito.DataLote);
            Assert.Empty(debito.DescricaoComplementarRetorno);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_LinhasDeSaldoDoRetornoReal_SaldoDisponivelViraSaldoAtual()
        {
            // Formato observado no extrato v2 de produção: as posições de saldo vêm como
            // lançamentos tipo "1" sem documento, seguidas de SD/RA/LC/LD e dos futuros (tipo "2").
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{
                ""numeroPaginaProximo"":0,""listaLancamento"": [
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":30092026,""numeroDocumento"":0,""codigoHistorico"":0,""textoDescricaoSubHistorico"":""SALDO ANTERIOR"",""codigoSubHistorico"":0,""valorLancamento"":40.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":1102026,""numeroDocumento"":0,""codigoHistorico"":0,""textoDescricaoSubHistorico"":""Saldo do dia"",""codigoSubHistorico"":0,""valorLancamento"":0.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":1102026,""numeroDocumento"":0,""codigoHistorico"":0,""textoDescricaoSubHistorico"":""Pix recebido"",""codigoSubHistorico"":0,""valorLancamento"":30.0,""indicadorSinalLancamento"":""C"",""textoIdentificadorUnicoTransacao"":""tx-pix""},
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":2102026,""numeroDocumento"":555,""codigoHistorico"":109,""textoDescricaoSubHistorico"":""Pagamento de boleto"",""codigoSubHistorico"":25001,""valorLancamento"":200.0,""indicadorSinalLancamento"":""D""},
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":2102026,""numeroDocumento"":0,""codigoHistorico"":0,""textoDescricaoSubHistorico"":""Saldo do dia"",""codigoSubHistorico"":0,""valorLancamento"":0.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""1"",""dataLancamento"":2102026,""numeroDocumento"":0,""codigoHistorico"":999,""textoDescricaoSubHistorico"":""S A L D O"",""codigoSubHistorico"":25001,""valorLancamento"":130.0,""indicadorSinalLancamento"":""D""},
                    {""indicadorTipoLancamento"":""RA"",""dataLancamento"":2102026,""textoDescricaoSubHistorico"":""Invest. Resgate Autom"",""valorLancamento"":1130.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""SD"",""dataLancamento"":2102026,""textoDescricaoSubHistorico"":""Saldo Disponivel"",""valorLancamento"":1000.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""LC"",""dataLancamento"":2102026,""textoDescricaoSubHistorico"":""Limite Contratado"",""valorLancamento"":10000.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""LD"",""dataLancamento"":2102026,""textoDescricaoSubHistorico"":""Limite Disponivel"",""valorLancamento"":9870.0,""indicadorSinalLancamento"":""C""},
                    {""indicadorTipoLancamento"":""2"",""dataLancamento"":3112026,""numeroDocumento"":845,""codigoHistorico"":177,""textoDescricaoSubHistorico"":""EMPRESTIMO"",""valorLancamento"":1869.93,""indicadorSinalLancamento"":""D""}
                ]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 10, 2026, 1, 2, false);

            Assert.Equal(3, resultado.Count);
            var saldo = Assert.IsType<MovimentoItemExtratoSaldo>(resultado[0].MovimentoItem);
            Assert.Equal(40m, saldo.SaldoAnterior);
            Assert.Equal(1000m, saldo.SaldoAtual);
            Assert.Equal(10000m, saldo.SaldoLimite);
            var transacoes = resultado.Skip(1).Select(m => Assert.IsType<MovimentoItemExtratoTransacao>(m.MovimentoItem)).ToList();
            Assert.Equal(new[] { "Pix recebido", "Pagamento de boleto" }, transacoes.Select(t => t.Descricao));
        }

        [Fact]
        public async Task ConsultarExtratoAsync_PeriodoAnteriorAHoje_SaldoDisponivelPrevaleceSobreSaldoAtualContabil()
        {
            // Com data final anterior a hoje o BB envia "S A L D O" do fim do período e, antes do SD,
            // um tipo SA com o saldo contábil de hoje.
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[
                {""indicadorTipoLancamento"":""1"",""dataLancamento"":15102026,""numeroDocumento"":0,""codigoHistorico"":999,""textoDescricaoSubHistorico"":""S A L D O"",""valorLancamento"":0.0,""indicadorSinalLancamento"":""C""},
                {""indicadorTipoLancamento"":""SA"",""dataLancamento"":15102026,""textoDescricaoSubHistorico"":""Saldo Atual"",""valorLancamento"":130.0,""indicadorSinalLancamento"":""D""},
                {""indicadorTipoLancamento"":""SD"",""dataLancamento"":15102026,""textoDescricaoSubHistorico"":""Saldo Disponivel"",""valorLancamento"":1000.0,""indicadorSinalLancamento"":""C""},
                {""indicadorTipoLancamento"":""SA"",""dataLancamento"":15102026,""textoDescricaoSubHistorico"":""Saldo Atual"",""valorLancamento"":130.0,""indicadorSinalLancamento"":""D""}
            ]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 10, 2026, 1, 15, false);

            Assert.Equal(1000m, Assert.IsType<MovimentoItemExtratoSaldo>(Assert.Single(resultado).MovimentoItem).SaldoAtual);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_SemSaldoDisponivel_PrefereSaldoAtualContabilAoFimDoPeriodo()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[
                {""indicadorTipoLancamento"":""SA"",""dataLancamento"":15102026,""textoDescricaoSubHistorico"":""Saldo Atual"",""valorLancamento"":130.0,""indicadorSinalLancamento"":""D""},
                {""indicadorTipoLancamento"":""1"",""dataLancamento"":15102026,""numeroDocumento"":0,""codigoHistorico"":999,""textoDescricaoSubHistorico"":""S A L D O"",""valorLancamento"":0.0,""indicadorSinalLancamento"":""C""}
            ]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 10, 2026, 1, 15, false);

            Assert.Equal(-130m, Assert.IsType<MovimentoItemExtratoSaldo>(Assert.Single(resultado).MovimentoItem).SaldoAtual);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_SemSaldoDisponivel_UsaSaldoContabilDaConta()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[
                {""indicadorTipoLancamento"":""1"",""dataLancamento"":2102026,""numeroDocumento"":0,""codigoHistorico"":999,""textoDescricaoSubHistorico"":""S A L D O"",""valorLancamento"":130.0,""indicadorSinalLancamento"":""D""}
            ]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 10, 2026, null, null, false);

            Assert.Equal(-130m, Assert.IsType<MovimentoItemExtratoSaldo>(Assert.Single(resultado).MovimentoItem).SaldoAtual);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_SemPosicaoDeSaldo_RetornaSoTransacoes()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[
                {""indicadorTipoLancamento"":""1"",""dataLancamento"":1102026,""numeroDocumento"":0,""codigoHistorico"":0,""textoDescricaoSubHistorico"":""Saldo do dia"",""valorLancamento"":0.0,""indicadorSinalLancamento"":""C""},
                {""indicadorTipoLancamento"":""1"",""dataLancamento"":1102026,""numeroDocumento"":7,""codigoHistorico"":624,""textoDescricaoSubHistorico"":""Pix recebido"",""valorLancamento"":10.0,""indicadorSinalLancamento"":""C""}
            ]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 10, 2026, null, null, false);

            Assert.Equal("Pix recebido", Assert.IsType<MovimentoItemExtratoTransacao>(Assert.Single(resultado).MovimentoItem).Descricao);
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(250, "Pagamento de título")]
        [InlineData(624, "Pix recebido")]
        public async Task ConsultarExtratoAsync_HistoricoESubHistorico_PreservaCodigosEDescricoes(int codigoHistorico, string? descricaoHistorico)
        {
            var resposta = JsonConvert.SerializeObject(new
            {
                listaLancamento = new[]
                {
                    new
                    {
                        indicadorTipoLancamento = "1", dataLancamento = 1092026,
                        codigoHistorico, textoDescricaoHistorico = descricaoHistorico,
                        codigoSubHistorico = 12345, textoDescricaoSubHistorico = "Detalhe do lançamento",
                        textoInformacaoComplementar = "Informação adicional", valorLancamento = 150.25,
                        indicadorSinalLancamento = "C", textoIdentificadorUnicoTransacao = "tx-historico"
                    }
                }
            });
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, resposta));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false);

            var item = Assert.IsType<MovimentoItemExtratoTransacao>(Assert.Single(resultado).MovimentoItem);
            Assert.Equal("Detalhe do lançamento", item.Descricao);
            Assert.Equal("Informação adicional", item.DescricaoInformacaoComplementar);
            Assert.Equal("tx-historico", item.TransactionId);
            Assert.Collection(item.DescricaoComplementarRetorno,
                historico =>
                {
                    Assert.Equal(codigoHistorico.ToString(System.Globalization.CultureInfo.InvariantCulture), historico.Codigo);
                    Assert.Equal(descricaoHistorico, historico.Descricao);
                },
                subHistorico =>
                {
                    Assert.Equal("12345", subHistorico.Codigo);
                    Assert.Equal("Detalhe do lançamento", subHistorico.Descricao);
                });
        }

        [Fact]
        public async Task ConsultarExtratoAsync_HistoricoSemDescricao_PreservaCodigoSemUsarTextoDoSubHistorico()
        {
            // A OpenAPI v2 documenta codigoHistorico, mas não textoDescricaoHistorico.
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[{
                ""indicadorTipoLancamento"":""1"",""dataLancamento"":1092026,""indicadorSinalLancamento"":""D"",
                ""codigoHistorico"":250,""codigoSubHistorico"":12345,""textoDescricaoSubHistorico"":""Pagamento de boleto""
            }]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false);

            var item = Assert.IsType<MovimentoItemExtratoTransacao>(Assert.Single(resultado).MovimentoItem);
            Assert.Equal("Pagamento de boleto", item.Descricao);
            Assert.Collection(item.DescricaoComplementarRetorno,
                historico =>
                {
                    Assert.Equal("250", historico.Codigo);
                    Assert.Null(historico.Descricao);
                },
                subHistorico =>
                {
                    Assert.Equal("12345", subHistorico.Codigo);
                    Assert.Equal("Pagamento de boleto", subHistorico.Descricao);
                });
        }

        [Fact]
        public async Task ConsultarExtratoAsync_CodigosAusentes_NaoInventaCodigoZero()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, @"{""listaLancamento"":[{
                ""indicadorTipoLancamento"":""1"",""dataLancamento"":1092026,""indicadorSinalLancamento"":""C"",
                ""textoDescricaoSubHistorico"":""Pix recebido""
            }]}"));

            var resultado = await Criar(handler).ConsultarExtratoAsync(0, 9, 2026, null, null, false);

            var item = Assert.IsType<MovimentoItemExtratoTransacao>(Assert.Single(resultado).MovimentoItem);
            var descricao = Assert.Single(item.DescricaoComplementarRetorno);
            Assert.Null(descricao.Codigo);
            Assert.Equal("Pix recebido", descricao.Descricao);
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

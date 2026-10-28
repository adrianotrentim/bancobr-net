using System.Net;
using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.API.Operacoes;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json;
using Xunit;

namespace BancoBr.Tests.Operacoes
{
    public class OperacoesTests
    {
        private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static Correntista Origem(int banco) => new Correntista
        {
            Banco = banco, NumeroAgencia = 1234, NumeroConta = 56789, DVConta = "1", Nome = "Empresa fictícia",
            CPF_CNPJ = "11111111000111", TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ
        };
        private static Movimento Boleto() => new Movimento
        {
            NumeroDocumento = "1234", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 10,
            Favorecido = new Favorecido { Nome = "Favorecido", CPF_CNPJ = "11111111111" },
            MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = new string('1', 44), ValorCodigoBarra = 10 }
        };

        internal static BancoApi Criar(int banco, HttpMessageHandler handler)
        {
            var http = new HttpClient(handler); var token = new FakeOAuthTokenProvider(); var origem = Origem(banco);
            switch ((BancoEnum)banco)
            {
                case BancoEnum.Sicredi:
                    return BancoApi.ConectarSicredi(new ConfiguracaoAPI { Correntista = origem, AmbienteHomologacao = true }, null, token, http);
                case BancoEnum.BancoDoBrasil:
                    return new BancoApi(origem, new BancoBr.API.BancoDoBrasil.Pagamentos.PagamentoBoletoClient(http, token, "chave-ficticia", origem),
                        new BancoBr.API.BancoDoBrasil.Pagamentos.PagamentoConvenioClient(http, token, "chave-ficticia", origem),
                        new BancoBr.API.BancoDoBrasil.Pagamentos.PagamentoPixClient(http, token, "chave-ficticia", origem),
                        new BancoBr.API.BancoDoBrasil.Pagamentos.PagamentoTedClient(http, token, "chave-ficticia", origem),
                        new BancoBr.API.BancoDoBrasil.Extrato.ExtratoClient(http, token, "chave-ficticia", new Uri("https://example.test/v2/"), origem));
                case BancoEnum.Sicoob:
                    return new BancoApi(origem,
                        new BancoBr.API.Sicoob.Pagamentos.Boletos.PagamentoBoletoClient(http, token, "cliente", new Uri("https://example.test/v3/"), origem),
                        new BancoBr.API.Sicoob.Pagamentos.Convenios.PagamentoConvenioClient(http, token, "cliente", new Uri("https://example.test/v1/"), origem),
                        new BancoBr.API.Sicoob.Pagamentos.Pix.PagamentoPixClient(http, token, "cliente", new Uri("https://example.test/v2/"), origem),
                        new BancoBr.API.Sicoob.Pagamentos.Ted.TedClient(http, token, "cliente", new Uri("https://example.test/v1/"), origem),
                        new BancoBr.API.Sicoob.ContaCorrente.ExtratoClient(http, token, "cliente", new Uri("https://example.test/v4/"), origem));
                default:
                    var url = new Uri("https://example.test/v1/");
                    return new BancoApi(origem,
                        new BancoBr.API.Santander.Pagamentos.Boletos.PagamentoBoletoClient(http, token, "cliente", url, origem, "ws"),
                        new BancoBr.API.Santander.Pagamentos.Convenios.PagamentoConvenioClient(http, token, "cliente", url, origem, "ws"),
                        new BancoBr.API.Santander.Pagamentos.Pix.PagamentoPixClient(http, token, "cliente", url, origem, "ws"),
                        new BancoBr.API.Santander.Pagamentos.Ted.PagamentoTedClient(http, token, "cliente", url, origem, "ws"),
                        new BancoBr.API.Santander.ContaCorrente.ExtratoClient(http, token, "cliente", new Uri("https://example.test/extrato/"), origem),
                        comprovante: new BancoBr.API.Santander.Pagamentos.Comprovantes.ComprovanteClient(http, token, "cliente", url, origem, "ws", intervaloComprovante: TimeSpan.Zero));
            }
        }

        private static SequencedFakeHttpMessageHandler Envio(int banco)
        {
            switch ((BancoEnum)banco)
            {
                case BancoEnum.Sicoob:
                    return new SequencedFakeHttpMessageHandler(
                        (HttpStatusCode.OK, "{\"resultado\":{\"identificadorConsulta\":\"consulta\",\"valorPagamento\":10}}"),
                        (HttpStatusCode.OK, "{\"resultado\":{\"idPagamento\":321,\"situacaoPagamento\":\"AGENDADO\",\"valorPagamento\":10},\"extra\":\"preservado\"}"));
                case BancoEnum.Santander:
                    return new SequencedFakeHttpMessageHandler(
                        (HttpStatusCode.Created, "{\"id\":\"321\",\"status\":\"READY_TO_PAY\",\"totalValue\":10}"),
                        (HttpStatusCode.OK, "{\"id\":\"321\",\"status\":\"PENDING_CONFIRMATION\",\"paymentValue\":10,\"extra\":\"preservado\"}"));
                case BancoEnum.BancoDoBrasil:
                    return new SequencedFakeHttpMessageHandler((HttpStatusCode.Created,
                        "{\"estadoRequisicao\":4,\"lancamentos\":[{\"codigoIdentificadorPagamento\":321,\"indicadorAceite\":\"S\"}],\"extra\":\"preservado\"}"));
                default:
                    return new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"AGUARDANDO_APROVACAO\",\"extra\":\"preservado\"}"));
            }
        }

        [Theory]
        [InlineData(756, BancoBrSituacaoEnum.Agendado)]
        [InlineData(33, BancoBrSituacaoEnum.Agendado)]
        [InlineData(1, BancoBrSituacaoEnum.NaoIntegrado)]
        [InlineData(748, BancoBrSituacaoEnum.Agendado)]
        public async Task ChamadaAntiga_ModelosOriginais_PreservaInstanciaEEstados(int banco, BancoBrSituacaoEnum esperado)
        {
            var handler = Envio(banco); var movimento = Boleto();
            var resultado = await Criar(banco, handler).EnviarAgendamentoAsync(movimento, Id.ToString());
            Assert.Same(movimento, resultado);
            Assert.Equal(esperado, resultado.SituacaoBancoBr);
            Assert.NotEmpty(handler.Requests);
            var serializado = JsonConvert.SerializeObject(resultado);
            Assert.DoesNotContain("IdRequisicaoBancoBr", serializado);
            Assert.DoesNotContain("SituacaoOriginalBancoBr", serializado);
        }

        [Theory]
        [InlineData(756, SituacaoPagamento.Agendado, SituacaoAutorizacao.NaoInformada, 2)]
        [InlineData(33, SituacaoPagamento.EmProcessamento, SituacaoAutorizacao.NaoInformada, 2)]
        [InlineData(1, SituacaoPagamento.EmProcessamento, SituacaoAutorizacao.AguardandoAutorizacao, 1)]
        [InlineData(748, SituacaoPagamento.EmProcessamento, SituacaoAutorizacao.AguardandoAutorizacao, 1)]
        public async Task ChamadaNova_QuatroBancos_SeparaExecucaoAutorizacaoEPreservaRetorno(int banco, SituacaoPagamento situacao, SituacaoAutorizacao autorizacao, int etapas)
        {
            var handler = Envio(banco);
            var resultado = await Criar(banco, handler).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            Assert.Equal(situacao, resultado.Situacao);
            Assert.Equal(autorizacao, resultado.Autorizacao);
            Assert.Equal(Id, resultado.IdOperacao);
            Assert.Equal(etapas, resultado.RetornosBanco.Count);
            Assert.Contains("preservado", resultado.RetornosBanco.Last().Corpo);
            Assert.Equal(banco, resultado.Referencia.Banco);
            Assert.False(string.IsNullOrEmpty(resultado.Referencia.IdentificadorBanco));
        }

        [Theory]
        [InlineData(756, "{\"resultado\":{\"idPagamento\":321,\"situacaoPagamento\":\"PAGO\",\"valorPagamento\":10}}")]
        [InlineData(33, "{\"id\":\"321\",\"status\":\"PAYED\",\"paymentValue\":10}")]
        [InlineData(1, "{\"estadoPagamento\":\"PAGO\",\"valorPagamento\":10,\"codigoAutenticacaoPagamento\":\"ABC\"}")]
        [InlineData(748, "{\"idTransacao\":\"321\",\"status\":\"SUCESSO\",\"valorPagamento\":10}")]
        public async Task Consultar_ReferenciaSerializada_RecuperaAposNovaConexao(int banco, string resposta)
        {
            var envio = await Criar(banco, Envio(banco)).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            var referencia = JsonConvert.DeserializeObject<ReferenciaPagamento>(JsonConvert.SerializeObject(envio.Referencia))!;
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, resposta));
            var resultado = await Criar(banco, handler).ConsultarPagamentoAsync(referencia);
            Assert.Equal(SituacaoPagamento.Efetivado, resultado.Situacao);
            Assert.Equal(10, resultado.Movimento.ValorPagamento);
            Assert.Equal(Id, resultado.IdOperacao);
            Assert.Single(resultado.RetornosBanco);
        }

        [Fact]
        public async Task Consultar_ReferenciaDeOutroBanco_NaoEnviaHttp()
        {
            var envio = await Criar(748, Envio(748)).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            var handler = new SequencedFakeHttpMessageHandler();
            await Assert.ThrowsAsync<ArgumentException>(() => Criar(1, handler).ConsultarPagamentoAsync(envio.Referencia));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task Enviar_SemIdentidadePersistida_NaoEnviaHttp()
        {
            var handler = new SequencedFakeHttpMessageHandler();
            await Assert.ThrowsAsync<ArgumentException>(() => Criar(748, handler).EnviarPagamentoAsync(new RequisicaoPagamento { Movimento = Boleto() }));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ChamadaNova_Concorrencia_IsolaRetornosEntreOperacoes()
        {
            var tarefas = Enumerable.Range(1, 8).Select(async n =>
            {
                var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"RECEBIDO\",\"operacao\":" + n + "}"));
                await Task.Yield();
                var resultado = await Criar(748, handler).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Guid.NewGuid(), Movimento = Boleto() });
                Assert.Contains("\"operacao\":" + n, Assert.Single(resultado.RetornosBanco).Corpo);
            });
            await Task.WhenAll(tarefas);
        }

        [Fact]
        public async Task Sicoob_AceitoSemId_RecuperaPorIdentidadeAposSerializacao()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"resultado\":{\"identificadorConsulta\":\"consulta\",\"valorPagamento\":10}}"),
                (HttpStatusCode.Accepted, ""));
            var envio = await Criar(756, handler).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            Assert.Equal(SituacaoAutorizacao.AguardandoAutorizacao, envio.Autorizacao);
            Assert.Null(envio.Referencia.IdentificadorBanco);
            var referencia = JsonConvert.DeserializeObject<ReferenciaPagamento>(JsonConvert.SerializeObject(envio.Referencia))!;
            var consulta = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK,
                "{\"resultado\":{\"idPagamento\":321,\"situacaoPagamento\":\"Efetivado\",\"valorPagamento\":10}}"));
            var resultado = await Criar(756, consulta).ConsultarPagamentoAsync(referencia);
            Assert.Equal(SituacaoPagamento.Efetivado, resultado.Situacao);
            Assert.Equal("321", resultado.Referencia.IdentificadorBanco);
            Assert.Contains("/idempotency/comprovantes", consulta.Requests.Single().RequestUri!.AbsolutePath);
            Assert.Contains(Id.ToString(), consulta.Requests.Single().RequestUri!.AbsolutePath);
        }

        [Theory]
        [InlineData(756, "{\"resultado\":{\"saldoAtual\":\"100\",\"transacoes\":[{\"tipo\":\"CREDITO\",\"valor\":\"10\",\"data\":\"2026-10-28\",\"transactionId\":\"tx\"}]}}")]
        [InlineData(33, "{\"_content\":[{\"creditDebitType\":\"CREDITO\",\"amount\":10,\"transactionDate\":\"2026-10-28\",\"transactionId\":\"tx\"}]}")]
        [InlineData(1, "{\"numeroPaginaProximo\":0,\"listaLancamento\":[{\"indicadorTipoLancamento\":\"1\",\"dataLancamento\":28102026,\"indicadorSinalLancamento\":\"C\",\"valorLancamento\":10,\"textoIdentificadorUnicoTransacao\":\"tx\"}]}")]
        [InlineData(748, "{\"saldoAnterior\":90,\"pageNumber\":0,\"totalPages\":1,\"dtlMovimentos\":[{\"data\":\"2026-10-28\",\"valor\":10,\"saldo\":100,\"idMovimento\":\"tx\"}]}")]
        public async Task ExtratoDetalhado_QuatroBancos_PreservaMovimentosERetornos(int banco, string resposta)
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, resposta));
            var resultado = await Criar(banco, handler).ConsultarExtratoDetalhadoAsync(new RequisicaoExtrato { NumeroContaCorrente = 56789, Mes = 10, Ano = 2026 });
            var lancamento = Assert.Single(resultado.Lancamentos);
            Assert.Equal("tx", Assert.IsType<MovimentoItemExtratoTransacao>(lancamento.Movimento.MovimentoItem).TransactionId);
            Assert.Equal(resposta, Assert.Single(resultado.RetornosBanco).Corpo);
            if (banco == 748) Assert.Equal(100, lancamento.SaldoAposLancamento);
            else Assert.Null(lancamento.SaldoAposLancamento);
            Assert.Null(typeof(MovimentoItemExtratoTransacao).GetProperty("SaldoAposLancamento"));
        }

        [Fact]
        public async Task ComprovanteBB_RetornaAutenticacaoSemExigirPdf()
        {
            var envio = await Criar(1, Envio(1)).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"estadoPagamento\":\"PAGO\",\"codigoAutenticacaoPagamento\":\"ABC\"}"));
            var comprovante = await Criar(1, handler).ObterComprovanteAsync(envio.Referencia);
            Assert.Equal("ABC", comprovante.Autenticacao);
            Assert.Equal(SituacaoPagamento.Efetivado, comprovante.Pagamento.Situacao);
            Assert.Null(comprovante.Conteudo);
        }

        [Fact]
        public async Task ComprovanteSantander_RetornaLinkPeloMesmoContrato()
        {
            var envio = await Criar(33, Envio(33)).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            var pago = "{\"id\":\"321\",\"status\":\"PAYED\",\"transaction\":{\"code\":\"ABC\"}}";
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, pago), (HttpStatusCode.OK, pago),
                (HttpStatusCode.OK, "{\"paymentReceiptsFileRequests\":[{\"request\":{\"requestId\":\"req\"},\"file\":{\"statusInfo\":{\"statusCode\":\"AVAILABLE\"}}}]}"),
                (HttpStatusCode.OK, "{\"file\":{\"statusInfo\":{\"statusCode\":\"AVAILABLE\"},\"fileRepository\":{\"location\":\"https://example.test/comprovante.pdf\"}}}"));
            var comprovante = await Criar(33, handler).ObterComprovanteAsync(envio.Referencia);
            Assert.Equal("https://example.test/comprovante.pdf", comprovante.Link);
            Assert.Equal(SituacaoPagamento.Efetivado, comprovante.Pagamento.Situacao);
            Assert.Equal(4, comprovante.Pagamento.RetornosBanco.Count);
            Assert.Equal("PAYED", comprovante.Pagamento.SituacaoOriginal);
        }

        [Fact]
        public async Task ComprovanteSicredi_Pendente_NaoSolicitaPdf()
        {
            var envio = await Criar(748, Envio(748)).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"AGUARDANDO_APROVACAO\"}"));
            var comprovante = await Criar(748, handler).ObterComprovanteAsync(envio.Referencia);
            Assert.Null(comprovante.Conteudo);
            Assert.Equal(SituacaoAutorizacao.AguardandoAutorizacao, comprovante.Pagamento.Autorizacao);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task RetornoDesconhecido_PreservaDiagnosticoSemPresumirAutorizacao()
        {
            var envio = await Criar(748, Envio(748)).EnviarPagamentoAsync(new RequisicaoPagamento { IdOperacao = Id, Movimento = Boleto() });
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"status\":\"ESTADO_NOVO\",\"detalheNovo\":123}"));
            var consulta = await Criar(748, handler).ConsultarPagamentoAsync(envio.Referencia);
            Assert.Equal(SituacaoPagamento.Desconhecida, consulta.Situacao);
            Assert.Equal(SituacaoAutorizacao.NaoInformada, consulta.Autorizacao);
            Assert.Equal("ESTADO_NOVO", consulta.SituacaoOriginal);
            Assert.Contains("detalheNovo", consulta.RetornosBanco.Single().Corpo);
        }
    }
}

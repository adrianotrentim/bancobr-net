using System.Globalization;
using System.Net;
using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BancoBr.Tests.Sicredi
{
    public class SicrediRegressaoTests
    {
        private static BancoApi Criar(HttpMessageHandler handler) => BancoApi.ConectarSicredi(new ConfiguracaoAPI
        {
            AmbienteHomologacao = true,
            Correntista = new Correntista { Banco = (int)BancoEnum.Sicredi, NumeroAgencia = 100, NumeroConta = 0, DVConta = "1", CPF_CNPJ = "11111111000111" }
        }, null, new FakeOAuthTokenProvider(), new HttpClient(handler));

        [Theory]
        [InlineData("pagamento")]
        [InlineData("extrato")]
        [InlineData("dda")]
        public async Task Datas_CulturaComCalendarioBudista_EnviaPeriodoGregoriano(string operacao)
        {
            var anterior = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
                var resposta = operacao == "extrato" ? "{\"saldoAnterior\":0,\"dtlMovimentos\":[],\"pageNumber\":0,\"totalPages\":1}"
                    : operacao == "dda" ? "{\"boletos\":[]}" : "{\"status\":\"RECEBIDO\"}";
                var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, resposta));
                var banco = Criar(handler);
                if (operacao == "pagamento")
                {
                    await banco.EnviarAgendamentoAsync(new Movimento
                    {
                        NumeroDocumento = "teste", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 10,
                        MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = "74892950500006000001123100010108100200003107" }
                    }, "identidade");
                    Assert.Equal("2026-10-28", (string?)JObject.Parse(handler.Bodies.Single()!)["dataPagamento"]);
                }
                else
                {
                    if (operacao == "extrato") await banco.ConsultarExtratoAsync(0, 10, 2026, 28, 29);
                    else await banco.ConsultarDDAAsync(0, new DateTime(2026, 10, 28), new DateTime(2026, 10, 29), SituacaoBoletoEnum.EmAberto, TipoDataConsultaEnum.Vencimento);
                    var query = handler.Requests.Single().RequestUri!.Query;
                    Assert.Contains("dataInicio=2026-10-28", query);
                    Assert.Contains("dataFim=2026-10-29", query);
                }
            }
            finally { CultureInfo.CurrentCulture = anterior; }
        }

        [Theory]
        [InlineData("boleto")]
        [InlineData("tributo")]
        [InlineData("pix")]
        public async Task ConsultarAgendamento_SomenteId_RecuperaDadosDoPagamento(string operacao)
        {
            var resposta = new JObject
            {
                ["idTransacao"] = "transacao", ["status"] = "SUCESSO", ["dataPagamento"] = "2026-10-28",
                ["valorPagamento"] = 119.62m, ["identificadorPagamentoAssociado"] = "ERP-001"
            };
            MovimentoItem item;
            if (operacao == "pix")
            {
                item = new MovimentoItemTransferenciaPIX();
                resposta["chavePix"] = "teste@example.com";
                resposta["nomeBeneficiario"] = "Beneficiário fictício";
                resposta["documentoBeneficiario"] = "11111111111";
            }
            else
            {
                item = operacao == "boleto" ? new MovimentoItemPagamentoTituloCodigoBarra() : new MovimentoItemPagamentoConvenioCodigoBarra();
                resposta["codigoBarra"] = "81610000001196248692023101183215529722698854";
                if (operacao == "tributo") resposta["idFgts"] = "0181";
            }
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, resposta.ToString()));
            var movimento = new Movimento { NumeroDocumentoNoBanco = "transacao", MovimentoItem = item };
            var resultado = await Criar(handler).ConsultarAgendamentoAsync(movimento);
            Assert.Same(movimento, resultado);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.Situacao);
            Assert.Equal(119.62m, resultado.ValorPagamento);
            Assert.Equal(new DateTime(2026, 10, 28), resultado.DataPagamento);
            Assert.Equal("ERP-001", resultado.NumeroDocumento);
            Assert.Equal("transacao", resultado.NumeroDocumentoNoBanco);

            if (item is MovimentoItemPagamentoTituloCodigoBarra boleto)
                Assert.Equal((string?)resposta["codigoBarra"], boleto.CodigoBarras);
            if (item is MovimentoItemPagamentoConvenioCodigoBarra tributo)
            {
                Assert.Equal((string?)resposta["codigoBarra"], tributo.CodigoBarra);
                Assert.Equal("0181", tributo.IdentificadorFgts);
            }
            if (item is MovimentoItemTransferenciaPIX pix)
            {
                Assert.Equal("teste@example.com", pix.ChavePIX);
                Assert.Equal("Beneficiário fictício", resultado.Favorecido.Nome);
                Assert.Equal("11111111111", resultado.Favorecido.CPF_CNPJ);
                Assert.Equal("Beneficiário fictício", pix.TitularConfirmadoNome);
                Assert.Equal("11111111111", pix.TitularConfirmadoDocumento);
            }
        }

        [Fact]
        public async Task ConsultarAgendamento_RespostaParcial_PreservaDadosConhecidos()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK,
                "{\"status\":\"AGENDADO\",\"valorPagamento\":null,\"dataPagamento\":null,\"codigoBarra\":null}"));
            var item = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = "barras-conhecidas" };
            var movimento = new Movimento
            {
                NumeroDocumentoNoBanco = "id", NumeroDocumento = "ERP-001",
                DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 10, MovimentoItem = item
            };

            await Criar(handler).ConsultarAgendamentoAsync(movimento);
            Assert.Equal(10, movimento.ValorPagamento);
            Assert.Equal(new DateTime(2026, 10, 28), movimento.DataPagamento);
            Assert.Equal("ERP-001", movimento.NumeroDocumento);

            Assert.Equal("barras-conhecidas", item.CodigoBarras);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, movimento.Situacao);
        }

        private static MovimentoItem Item(string recurso)
        {
            if (recurso == "boletos") return new MovimentoItemPagamentoTituloCodigoBarra
                { CodigoBarras = "74892950500006000001123100010108100200003107" };
            if (recurso == "tributos/barras") return new MovimentoItemPagamentoConvenioCodigoBarra
                { CodigoBarra = "81610000001196248692023101183215529722698854" };
            return new MovimentoItemTransferenciaPIX { ChavePIX = "teste@example.com" };
        }

        [Theory]
        [InlineData("boletos", null)]
        [InlineData("boletos", "outra-chave")]
        [InlineData("tributos/barras", null)]
        [InlineData("tributos/barras", "outra-chave")]
        [InlineData("pix", null)]
        [InlineData("pix", "outra-chave")]
        public async Task EnviarAgendamento_MovimentoRestaurado_ConsultaReferenciaSemNovoPagamento(string recurso, string? chave)
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"status\":\"RECEBIDO\"}"),
                (HttpStatusCode.OK, "{\"status\":\"SUCESSO\"}"));
            var banco = Criar(handler);
            var movimento = new Movimento
            {
                NumeroDocumento = "ERP-001", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 10,
                Favorecido = new Favorecido { CPF_CNPJ = "11111111111" }, MovimentoItem = Item(recurso)
            };
            await banco.EnviarAgendamentoAsync(movimento, "referencia-original");
            // Uma nova instância, como na leitura dos dados persistidos pelo consumidor.
            var restaurado = new Movimento
            {
                NumeroDocumento = movimento.NumeroDocumento, NumeroDocumentoNoBanco = movimento.NumeroDocumentoNoBanco,
                DataPagamento = movimento.DataPagamento, ValorPagamento = movimento.ValorPagamento,
                Favorecido = movimento.Favorecido, MovimentoItem = Item(recurso)
            };
            var resultado = await banco.EnviarAgendamentoAsync(restaurado, chave);

            Assert.Same(restaurado, resultado);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
            Assert.EndsWith("/" + recurso + "/referencia-original", handler.Requests[1].RequestUri!.AbsolutePath);
            Assert.Equal("referencia-original", resultado.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.Situacao);
        }

        [Fact]
        public async Task EnviarAgendamento_ReferenciaNaoEncontrada_NaoCriaOutroPagamento()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.NotFound, "{}"));
            var movimento = new Movimento { NumeroDocumentoNoBanco = "referencia-original", MovimentoItem = Item("boletos") };
            await Assert.ThrowsAsync<BancoBr.API.Core.Errors.BancoApiException>(() => Criar(handler).EnviarAgendamentoAsync(movimento));
            Assert.Equal(HttpMethod.Get, Assert.Single(handler.Requests).Method);
            Assert.Equal("referencia-original", movimento.NumeroDocumentoNoBanco);
        }

        private static Movimento PixDadosBancarios() => new Movimento
        {
            NumeroDocumento = "ERP-PIX", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 51.22m,
            Favorecido = new Favorecido { Nome = "Favorecido fictício", CPF_CNPJ = "111.111.111-11" },
            MovimentoItem = new MovimentoItemTransferenciaPIX
            {
                Ispb = "1234567", NumeroAgencia = 101, NumeroConta = 1111, DVConta = "9"
            }
        };

        [Theory]
        [InlineData(TipoContaEnum.ContaCorrente, "CORRENTE")]
        [InlineData(TipoContaEnum.ContaPoupanca, "POUPANCA")]
        [InlineData(TipoContaEnum.ContaPagamento, "PAGAMENTO")]
        public async Task EnviarPix_DadosBancarios_UsaModelosExistentesERotasDaModalidade(TipoContaEnum tipo, string esperado)
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"status\":\"AGENDADO\"}"),
                (HttpStatusCode.OK, "{\"status\":\"AGENDADO\"}"), (HttpStatusCode.OK, ""));
            var banco = Criar(handler);
            var movimento = PixDadosBancarios();
            ((MovimentoItemTransferenciaPIX)movimento.MovimentoItem).TipoConta = tipo;
            await banco.EnviarAgendamentoAsync(movimento, "referencia-pix");
            await banco.ConsultarAgendamentoAsync(movimento);
            await banco.CancelarAgendamentoAsync(movimento);

            var body = JObject.Parse(handler.Bodies[0]!);
            Assert.EndsWith("/pix/dados-bancarios", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("0101", (string?)body["agenciaBeneficiario"]);
            Assert.Equal("11119", (string?)body["contaBeneficiario"]);
            Assert.Equal("01234567", (string?)body["ispbBeneficiario"]);
            Assert.Equal(esperado, (string?)body["tipoContaBeneficiario"]);
            Assert.Equal("Favorecido fictício", (string?)body["nomeBeneficiario"]);
            Assert.Equal("11111111111", (string?)body["documentoBeneficiario"]);
            Assert.Equal("referencia-pix", (string?)body["idTransacao"]);
            Assert.Null(body["chavePix"]);
            Assert.EndsWith("/pix/referencia-pix", handler.Requests[1].RequestUri!.AbsolutePath);
            Assert.EndsWith("/pix/cancelamentos", handler.Requests[2].RequestUri!.AbsolutePath);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, movimento.Situacao);
        }

        [Theory]
        [InlineData("sem-ispb")]
        [InlineData("ispb-longo")]
        [InlineData("ispb-nao-numerico")]
        [InlineData("sem-nome")]
        [InlineData("sem-documento")]
        [InlineData("sem-conta")]
        [InlineData("agencia-longa")]
        [InlineData("conta-longa")]
        public async Task EnviarPix_DadosBancariosInvalidos_NaoEnvia(string cenario)
        {
            var handler = new SequencedFakeHttpMessageHandler();
            var movimento = PixDadosBancarios();
            var pix = (MovimentoItemTransferenciaPIX)movimento.MovimentoItem;
            switch (cenario)
            {
                case "sem-ispb": pix.Ispb = ""; pix.Banco = 748; break;
                case "ispb-longo": pix.Ispb = "123456789"; break;
                case "ispb-nao-numerico": pix.Ispb = "ISPB"; break;
                case "sem-nome": movimento.Favorecido.Nome = ""; break;
                case "sem-documento": movimento.Favorecido.CPF_CNPJ = ""; break;
                case "sem-conta": pix.NumeroConta = 0; break;
                case "agencia-longa": pix.NumeroAgencia = 10000; break;
                case "conta-longa": pix.NumeroConta = long.MaxValue; pix.DVConta = "12"; break;
            }
            await Assert.ThrowsAsync<ArgumentException>(() => Criar(handler).EnviarAgendamentoAsync(movimento));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [InlineData("boletos", "multipag.boleto")]
        [InlineData("tributos/barras", "multipag.tributos")]
        [InlineData("pix", "multipag.pix")]
        public async Task Fachada_EnviarECancelar_UsaClienteEEscoposDaModalidade(string recurso, string escopo)
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"access_token\":\"ficticio\",\"expires_in\":3600}"),
                (HttpStatusCode.OK, "{\"status\":\"AGENDADO\"}"), (HttpStatusCode.OK, ""));
            var banco = BancoApi.ConectarSicredi(new ConfiguracaoAPI
            {
                AmbienteHomologacao = true, ClientId = "id-ficticio", ClientSecret = "segredo-ficticio",
                Correntista = new Correntista { NumeroAgencia = 100, NumeroConta = 0, DVConta = "1", CPF_CNPJ = "11111111000111" }
            }, null, null, new HttpClient(handler));
            var movimento = new Movimento
            {
                NumeroDocumento = "ERP-001", DataPagamento = new DateTime(2026, 10, 28), ValorPagamento = 10,
                Favorecido = new Favorecido { CPF_CNPJ = "11111111111" }, MovimentoItem = Item(recurso)
            };
            await banco.EnviarAgendamentoAsync(movimento, "identidade-erp");
            await banco.CancelarAgendamentoAsync(movimento);
            Assert.Contains(escopo + ".pagar", handler.Bodies[0]);
            Assert.Contains(escopo + ".consultar", handler.Bodies[0]);
            var rota = "/sb/multipag-pagamento-sandbox/v1/pagamentos/" + recurso;
            Assert.Equal(rota + (recurso == "pix" ? "/chave" : ""), handler.Requests[1].RequestUri!.AbsolutePath);
            Assert.Equal(rota + "/cancelamentos", handler.Requests[2].RequestUri!.AbsolutePath);
            Assert.Equal("identidade-erp", (string?)JObject.Parse(handler.Bodies[1]!)["idTransacao"]);
            Assert.Equal("identidade-erp", (string?)JObject.Parse(handler.Bodies[2]!)["idTransacao"]);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, movimento.Situacao);
        }

        private sealed class PdfHandler : HttpMessageHandler
        {
            internal readonly byte[] Bytes = { 37, 80, 68, 70, 45, 255, 0, 128 };
            internal string? Caminho;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Caminho = request.RequestUri!.AbsolutePath;
                if (!Caminho.EndsWith("/comprovantes"))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"status\":\"SUCESSO\"}") });
                Assert.Equal("application/pdf", request.Headers.Accept.Single().MediaType);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Bytes) });
            }
        }

        [Theory]
        [InlineData("boletos")]
        [InlineData("tributos/barras")]
        [InlineData("pix")]
        public async Task Fachada_ConsultarComprovante_PreservaPdfEDistribuiPorModalidade(string recurso)
        {
            var handler = new PdfHandler();
            var movimento = new Movimento { NumeroDocumentoNoBanco = "id", MovimentoItem = Item(recurso) };
            var obtido = await Criar(handler).ConsultarComprovanteAsync(movimento);
            Assert.True(obtido);
            Assert.Equal(handler.Bytes, movimento.ArquivoComprovantePDF);
            Assert.Equal("/sb/multipag-pagamento-sandbox/v1/pagamentos/" + recurso + "/id/comprovantes", handler.Caminho);
        }

        [Fact]
        public async Task Fachada_ConsultarSaldo_UsaContratoDeSaldo()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, "{\"saldoAtual\":400}"));
            var movimento = await Criar(handler).ConsultarSaldoAsync();
            Assert.Equal(400, Assert.IsType<MovimentoItemExtratoSaldo>(movimento.MovimentoItem).SaldoAtual);
            Assert.Equal("/sb/contacorrente/v1/saldos", handler.Requests.Single().RequestUri!.AbsolutePath);
        }
    }
}

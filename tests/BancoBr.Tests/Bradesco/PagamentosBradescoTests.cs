using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BancoBr.API;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Bradesco.ContaCorrente;
using BancoBr.API.Bradesco.Pagamentos;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BancoBr.Tests.Bradesco
{
    public class PagamentosBradescoTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 11, 10, 30, 15);

        private static Correntista Origem() => new Correntista
        {
            Banco = (int)BancoEnum.BradescoSA, NumeroAgencia = 2856, DVAgencia = "0", NumeroConta = 50035, DVConta = "6",
            CPF_CNPJ = "12.345.678/0001-90", Nome = "EMPRESA TESTE LTDA"
        };

        private static IAccessTokenProvider Token() => new StaticAccessTokenProvider("token");

        private static Movimento Ted() => new Movimento
        {
            NumeroDocumento = "ABC123", DataPagamento = new DateTime(2026, 11, 11), ValorPagamento = 676.42m,
            Favorecido = new Favorecido { CPF_CNPJ = "05.171.355/0002-48", Nome = "MARIA DE SOUZA MATOS" },
            MovimentoItem = new MovimentoItemTransferenciaTED
            {
                Banco = 341, NumeroAgencia = 6234, NumeroConta = 5475, DVConta = "4", CodigoFinalidadeTED = FinalidadeTEDEnum.CreditoEmConta
            }
        };

        private static Movimento Pix(MovimentoItemTransferenciaPIX item) => new Movimento
        {
            NumeroDocumento = "TransfAPI0001", DataPagamento = new DateTime(2026, 11, 11), ValorPagamento = 50m,
            Favorecido = new Favorecido { CPF_CNPJ = "099.999.022-91", Nome = "KLEBER ADILSON" }, MovimentoItem = item
        };

        private static Movimento Boleto() => new Movimento
        {
            NumeroDocumento = "22", DataPagamento = new DateTime(2026, 11, 11), ValorPagamento = 0,
            MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = "23791131100000001002856095127000025102226520" }
        };

        private const string RespostaValida = "{\"status\":200,\"transacao\":\"CBCAIABF\",\"dataVencimento\":20261231,\"codBancoConsulta\":237,\"nmBanco\":\"BANCO BRADESCO S.A.\"," +
            "\"nmCedente\":\"CONTA AMBIENTE TU\",\"valorTitulo\":100.99,\"numeroCtrlCip\":\"0237CP01251271370358\",\"codBarras\":\"23791131100000001002856095127000025102226520\"," +
            "\"cnpjSacado\":9538989000166,\"nomeSacado\":\"TESTE BE\",\"cindcdAltVlr\":\"3\"}";

        private const string RespostaPreEfetivacao = "{\"status\":200,\"nroProtocolo\":0,\"nomeCedente\":\"CONTA AMBIENTE TU\",\"valorTitulo\":100.99,\"valorDesconto\":1.00,\"valorAbatimento\":0.50," +
            "\"valorBonificacao\":0,\"valorMulta\":2.00,\"valorJuros\":0.75,\"valorCobrado\":102.24,\"dataVctoTitlo\":20261231,\"linhaDigitavel1\":\"23793.99500 90257.006776 52007.555700 4\"," +
            "\"linhaDigitavel2\":99440000010099,\"cnpjCpfSacado\":60746948000112,\"nomePagador\":\"TTTTTTTT\",\"nomeBancoBenef\":\"BANCO BRADESCO S.A.\"}";

        private static string RespostaEfetivacao(string quitacao) =>
            "{\"status\":200,\"transacao\":\"CBCAIAAZ\",\"nroProtocolo\":4567,\"valorCobrado\":102.24,\"dataQuitacao\":" + quitacao + "}";

        private static PagamentoBoletoClient ClienteBoleto(HttpMessageHandler handler, Correntista? origem = null) =>
            new PagamentoBoletoClient(new HttpClient(handler), Token(), origem ?? Origem(), null, () => Agora);

        private static BancoApi Fachada(PagamentoBoletoApiBase? boleto = null, PagamentoPixApiBase? pix = null, PagamentoTedApiBase? ted = null, ContaCorrenteApiBase? extrato = null, PagamentoConvenioApiBase? convenio = null)
        {
            var construtor = typeof(BancoApi).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (BancoApi)construtor.Invoke(new object?[] { Origem(), boleto, convenio, pix, ted, extrato, null, null, null, null });
        }

        /// <summary>Aceita aspas simples para os JSONs de exemplo ficarem legíveis.</summary>
        private static string Json(string texto) => texto.Replace('\'', '"');

        #region ::. TED .::

        [Fact]
        public async Task PagarTed_MapeiaCamposDocumentadosEGuardaChaveDoBanco()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"codigoDeRetorno\":23,\"codigoDeErro\":null,\"mensagem\":\"OPERACAO EFETUADA COM SUCESSO\",\"bancoDestinatario\":\"341\",\"chaveUnicaParaApi\":\"29694332025-03-06-15.57.10.088927\"}");
            var client = new PagamentoTedClient(new HttpClient(handler), Token(), Origem());
            var movimento = Ted();

            await client.PagarTedAsync(movimento, "ignorada");

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://openapi.bradesco.com.br/transferencia/ted/v1/efetiva", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
            var corpo = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal(2856, (int)corpo["agenciaRemetente"]!);
            Assert.Equal(500356, (long)corpo["contaRemetenteComDigito"]!);
            Assert.Equal("J", (string)corpo["tipoDePessoaRemetente"]!);
            Assert.Equal(341, (int)corpo["bancoDestinatario"]!);
            Assert.Equal(54754, (long)corpo["contaDestinatario"]!);
            Assert.Equal("005171355", (string)corpo["numeroInscricao"]!);
            Assert.Equal("00002", (string)corpo["numeroFilial"]!);
            Assert.Equal("48", (string)corpo["numeroControle"]!);
            Assert.Equal("J", (string)corpo["tipodePessoaDestinatario"]!);
            Assert.Equal("E", (string)corpo["tipoDeDoc"]!);
            Assert.Equal(10, (int)corpo["finalidadeDaTransferencia"]!);
            Assert.Equal("11.11.2026", (string)corpo["dataMovimento"]!);
            Assert.Equal(676.42m, (decimal)corpo["valorDaTransferencia"]!);
            Assert.Equal("29694332025-03-06-15.57.10.088927", movimento.NumeroDocumentoNoBanco);
            Assert.Equal("2969433", ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).NumeroControleIF);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, movimento.Situacao);
        }

        [Fact]
        public async Task PagarTed_MesmoDocumentoDaOrigem_UsaMesmaTitularidade()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"codigoDeRetorno\":23,\"chaveUnicaParaApi\":\"12345672026-11-11-10.00.00.000000\"}");
            var movimento = Ted();
            movimento.Favorecido!.CPF_CNPJ = "12345678000190";

            await new PagamentoTedClient(new HttpClient(handler), Token(), Origem()).PagarTedAsync(movimento, "x");

            Assert.Equal("D", (string)JObject.Parse(handler.LastRequestBody!)["tipoDeDoc"]!);
        }

        [Fact]
        public async Task PagarTed_RetornoDiferenteDeSucesso_AplicaRejeicao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"codigoDeRetorno\":29,\"codigoDeErro\":\"0029\",\"mensagem\":\"Grade indisponivel\"}");
            var movimento = Ted();

            await new PagamentoTedClient(new HttpClient(handler), Token(), Origem()).PagarTedAsync(movimento, "x");

            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, movimento.Situacao);
            Assert.Equal("Grade indisponivel", movimento.DetalheRejeicao);
            Assert.Null(movimento.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task PagarTed_SemFinalidade_NaoEnvia()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
            var movimento = Ted();
            ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).CodigoFinalidadeTED = FinalidadeTEDEnum.NaoAplicavel;

            await Assert.ThrowsAsync<InvalidOperationException>(() => new PagamentoTedClient(new HttpClient(handler), Token(), Origem()).PagarTedAsync(movimento, "x"));

            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task PagarTed_ErroDoBanco_PreservaCodigoEMensagem()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.PreconditionFailed, "{\"codigoDeErro\":\"0008\",\"mensagem\":\"Grade de horario fechada\"}");

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => new PagamentoTedClient(new HttpClient(handler), Token(), Origem()).PagarTedAsync(Ted(), "x"));

            Assert.Equal(412, ex.HttpStatusCode);
            Assert.Equal("0008", ex.Mensagens[0].Codigo);
            Assert.Equal("Grade de horario fechada", ex.Mensagens[0].Mensagem);
        }

        [Theory]
        [InlineData("ACEITA", BancoBrSituacaoEnum.Efetivado)]
        [InlineData("DEVOLVIDA", BancoBrSituacaoEnum.Rejeitado)]
        [InlineData("EM PROCESSAMENTO", BancoBrSituacaoEnum.NaoIntegrado)]
        public async Task ConsultarTed_MapeiaStatusEUsaChaveDaEfetivacao(string status, BancoBrSituacaoEnum esperado)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"chaveUnicaTed\":\"2969433\",\"bancoDestinatario\":341,\"statusMensagem\":\"" + status + "\",\"descricaoDaDevolucao\":\"AG.OU CTA DEST. INVALIDA\"}");
            var movimento = Ted();
            movimento.NumeroDocumentoNoBanco = "29694332025-03-06-15.57.10.088927";

            await new PagamentoTedClient(new HttpClient(handler), Token(), Origem()).ConsultarTedAsync(movimento);

            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Equal("https://openapi.bradesco.com.br/transferencia/ted/v1/consulta?numeroDocumento=2969433&dataOperacao=06.03.2025", handler.LastRequest.RequestUri!.ToString());
            Assert.Equal(esperado, movimento.Situacao);
            Assert.Equal(esperado == BancoBrSituacaoEnum.Rejeitado ? "AG.OU CTA DEST. INVALIDA" : null, movimento.DetalheRejeicao);
        }

        [Fact]
        public async Task ConsultarTed_ChaveDaConsulta_UsaDataComPontos()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"statusMensagem\":\"ACEITA\"}");
            var movimento = Ted();
            movimento.NumeroDocumentoNoBanco = "278611429.11.2024";

            await new PagamentoTedClient(new HttpClient(handler), Token(), Origem()).ConsultarTedAsync(movimento);

            Assert.EndsWith("numeroDocumento=2786114&dataOperacao=29.11.2024", handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task CancelarTed_NaoSuportado()
        {
            var client = new PagamentoTedClient(new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK)), Token(), Origem());

            await Assert.ThrowsAsync<NotSupportedException>(() => client.CancelarAgendamentoAsync(Ted(), "x"));
        }

        #endregion

        #region ::. Pix .::

        [Fact]
        public async Task PagarPix_PorChave_EnviaPagadorPorAgenciaContaEGuardaE2e()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"recebedor\":{\"cpfCnpj\":\"09999902291969\",\"tipoChave\":\"EMAIL\",\"nomeFavorecido\":\"KLEBER ADILSON\"},\"valor\":\"50.00\"," +
                "\"e2e\":\"E60746948202211301715L2856oOXfn8\",\"dataCriacao\":\"2022-11-30T17:15:08.537Z\",\"status\":\"CONCLUIDO\",\"mensagem\":\"Transação realizada com sucesso\"}");
            var item = new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "kleber@example.com" };
            var movimento = Pix(item);

            await new PagamentoPixClient(new HttpClient(handler), Token(), Origem()).PagarComIniciacaoAsync(movimento);

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://qrpix.bradesco.com.br/v1/spi/solicitar-transferencia", handler.LastRequest.RequestUri!.ToString());
            var corpo = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal("AGENCIACONTA", (string)corpo["pagador"]!["tipoChave"]!);
            Assert.Equal("2856", (string)corpo["pagador"]!["agencia"]!);
            Assert.Equal("500356", (string)corpo["pagador"]!["conta"]!);
            Assert.Equal("EMAIL", (string)corpo["recebedor"]!["tipoChave"]!);
            Assert.Equal("kleber@example.com", (string)corpo["recebedor"]!["chavePix"]!);
            Assert.Equal("09999902291", (string)corpo["recebedor"]!["cpfCnpj"]!);
            Assert.Equal("TransfAPI0001", (string)corpo["idtransacao"]!);
            Assert.Equal("50.00", (string)corpo["valor"]!);
            Assert.Equal("E60746948202211301715L2856oOXfn8", movimento.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
            Assert.Equal("KLEBER ADILSON", item.TitularConfirmadoNome);
            Assert.Equal(2022, movimento.Horario!.Value.Year);
        }

        [Fact]
        public async Task PagarPix_TelefoneECpf_NormalizamChaveSemMascara()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":\"EM_PROCESSAMENTO\",\"e2e\":\"E60746948202211301715L2856oOXfn8\"}");
            var client = new PagamentoPixClient(new HttpClient(handler), Token(), Origem());

            var telefone = Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Telefone, ChavePIX = "(11) 99999-8888" });
            await client.PagarComIniciacaoAsync(telefone);
            Assert.Equal("+5511999998888", (string)JObject.Parse(handler.LastRequestBody!)["recebedor"]!["chavePix"]!);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, telefone.Situacao);

            await client.PagarComIniciacaoAsync(Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_CPF_CNPJ, ChavePIX = "099.999.022-91" }));
            var recebedor = JObject.Parse(handler.LastRequestBody!)["recebedor"]!;
            Assert.Equal("CPFCNPJ", (string)recebedor["tipoChave"]!);
            Assert.Equal("09999902291", (string)recebedor["chavePix"]!);
        }

        [Fact]
        public async Task PagarPix_PorAgenciaEConta_UsaIspbDoBancoETipoDeConta()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":\"CONCLUIDO\",\"e2e\":\"E60746948202211301717L2856RaWTN0\"}");
            var movimento = Pix(new MovimentoItemTransferenciaPIX { Banco = 237, NumeroAgencia = 3987, NumeroConta = 200958, DVConta = "7", TipoConta = TipoContaEnum.ContaPoupanca });

            await new PagamentoPixClient(new HttpClient(handler), Token(), Origem()).PagarComIniciacaoAsync(movimento);

            var recebedor = JObject.Parse(handler.LastRequestBody!)["recebedor"]!;
            Assert.Equal("AGENCIACONTA", (string)recebedor["tipoChave"]!);
            Assert.Equal("60746948", (string)recebedor["ispb"]!);
            Assert.Equal("237", (string)recebedor["banco"]!);
            Assert.Equal("3987", (string)recebedor["agencia"]!);
            Assert.Equal("200958", (string)recebedor["conta"]!);
            Assert.Equal("7", (string)recebedor["digitoConta"]!);
            Assert.Equal("CONTA_POUPANCA", (string)recebedor["tipoConta"]!);
            Assert.Null(recebedor["chavePix"]);
        }

        [Fact]
        public async Task PagarPix_Rejeitado_AplicaMensagem()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":\"REJEITADO\",\"mensagem\":\"Saldo insuficiente\"}");
            var movimento = Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "a@b.com" });

            await new PagamentoPixClient(new HttpClient(handler), Token(), Origem()).PagarComIniciacaoAsync(movimento);

            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, movimento.Situacao);
            Assert.Equal("Saldo insuficiente", movimento.DetalheRejeicao);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("com espaço")]
        [InlineData("123456789012345678901234567890123456")]
        public async Task PagarPix_NumeroDocumentoInvalido_NaoEnvia(string? numeroDocumento)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
            var movimento = Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "a@b.com" });
            movimento.NumeroDocumento = numeroDocumento;

            await Assert.ThrowsAsync<ArgumentException>(() => new PagamentoPixClient(new HttpClient(handler), Token(), Origem()).PagarComIniciacaoAsync(movimento));

            Assert.Null(handler.LastRequest);
        }

        [Theory]
        [InlineData("E60746948202211301715L2856oOXfn8", "/v1/spi/consulta/transferencia/TransfAPI0001/E60746948202211301715L2856oOXfn8")]
        [InlineData("curto", "/v1/spi/consulta/transferencia/TransfAPI0001")]
        public async Task ConsultarPix_UsaE2eQuandoValido(string e2e, string caminho)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":\"CONCLUIDO\",\"dataEfetivacao\":\"2022-11-30T17:15:19.807Z\"}");
            var movimento = Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "a@b.com" });
            movimento.NumeroDocumentoNoBanco = e2e;

            await new PagamentoPixClient(new HttpClient(handler), Token(), Origem()).ConsultarPagamentoAsync(movimento);

            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Equal(caminho, handler.LastRequest.RequestUri!.AbsolutePath);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
        }

        [Fact]
        public async Task PixBradesco_SemIniciacaoQrCodeOuConfirmacao_NaoSuportado()
        {
            var client = new PagamentoPixClient(new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK)), Token(), Origem());
            var movimento = Pix(new MovimentoItemTransferenciaPIX());

            await Assert.ThrowsAsync<NotSupportedException>(() => client.IniciarPagamentoAsync(movimento));
            await Assert.ThrowsAsync<NotSupportedException>(() => client.ConfirmarPagamentoAsync(movimento));
            await Assert.ThrowsAsync<NotSupportedException>(() => client.PagarViaQrCodeAsync(movimento));
        }

        #endregion

        #region ::. Boleto .::

        [Fact]
        public async Task ConsultarBoleto_ValidaTituloEPreEfetiva()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao));
            var movimento = Boleto();

            await ClienteBoleto(handler).ConsultarBoletoAsync(movimento);

            Assert.Equal("https://openapi.bradesco.com.br/boleto/pagamento-cobranca/v1/cobranca-valida-titulo-pagamento/validaTituloPagamento", handler.Requests[0].RequestUri!.ToString());
            var valida = JObject.Parse(handler.Bodies[0]!);
            Assert.Equal(1, (int)valida["tipoEntrada"]!);
            Assert.Equal("000000000023791131100000001002856095127000025102226520", (string)valida["dadosEntrada"]!);
            Assert.Equal(237, (int)valida["bancoTitulo"]!);
            Assert.Equal(2856, (int)valida["agenciaDeb"]!);
            Assert.Equal(50035, (int)valida["contaDeb"]!);

            Assert.EndsWith("cobranca-pre-efetivacao/pre-efetivacao-pagamento", handler.Requests[1].RequestUri!.AbsolutePath);
            var pre = JObject.Parse(handler.Bodies[1]!);
            Assert.Equal(20261231, (int)pre["dataVencimento"]!);
            Assert.Equal(100.99m, (decimal)pre["valorTitulo"]!);
            Assert.Equal(20261111, (int)pre["dataMovimento"]!);
            Assert.Equal(103015, (int)pre["horaTransacao"]!);
            Assert.Equal("0237CP01251271370358", (string)pre["numeroCtrlCip"]!);
            Assert.Equal("6", (string)pre["digitoConta"]!);

            var item = (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem;
            Assert.Equal("0237CP01251271370358", item.IdentificadorConsulta);
            Assert.Equal(new DateTime(2026, 12, 31), item.DataVencimento);
            Assert.Equal(100.99m, item.ValorCodigoBarra);
            Assert.Equal(1.50m, item.Desconto);
            Assert.Equal(2.75m, item.Acrescimo);
            Assert.False(item.AceitaValorDivergente);
            Assert.Equal("TTTTTTTT", item.PagadorConfirmadoNome);
            Assert.Equal("60746948000112", item.PagadorConfirmadoDocumento);
            Assert.Equal(102.24m, movimento.ValorPagamento);
            Assert.Null(movimento.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task ConsultarBoleto_LinhaDigitavel_EnviaComMascaraETipo2()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao));
            var movimento = Boleto();
            movimento.MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { NumeroLinhaDigitavel = "34119696490136076857401489783306470199756341698" };

            await ClienteBoleto(handler).ConsultarBoletoAsync(movimento);

            var valida = JObject.Parse(handler.Bodies[0]!);
            Assert.Equal(2, (int)valida["tipoEntrada"]!);
            Assert.Equal("34119.69649 01360.768574 01489.783306 4 70199756341698", (string)valida["dadosEntrada"]!);
            Assert.Equal(341, (int)valida["bancoTitulo"]!);
        }

        [Fact]
        public async Task PagarBoletoComConsulta_ValidaPreEfetivaEEfetivaComTransactionIdDoMovimento()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao), (HttpStatusCode.OK, RespostaEfetivacao("20261111")));
            var movimento = Boleto();

            await ClienteBoleto(handler).PagarBoletoComConsultaAsync(movimento, Guid.NewGuid());

            Assert.Equal(3, handler.Requests.Count);
            Assert.EndsWith("cobranca-efetivacao/solicitacao/executar", handler.Requests[2].RequestUri!.AbsolutePath);
            var efetivacao = JObject.Parse(handler.Bodies[2]!);
            Assert.Equal(22, (long)efetivacao["transactionId"]!);
            Assert.Equal("1", (string)efetivacao["indicadorFuncao"]!);
            Assert.Equal(12345678, (long)efetivacao["cnpjCpfPtdor"]!);
            Assert.Equal(1, (int)efetivacao["filialCnpjPtdor"]!);
            Assert.Equal(90, (int)efetivacao["ctrlCnpjPtdor"]!);
            Assert.Equal("0237CP01251271370358", (string)efetivacao["numeroCtrlCip"]!);
            Assert.Equal(20261231, (int)efetivacao["dataVencimento"]!);
            Assert.Equal(100.99m, (decimal)efetivacao["valorTitulo"]!);
            Assert.Equal(2856, (int)efetivacao["codigoAgencia"]!);
            Assert.Equal("22", movimento.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
            Assert.Equal("4567", ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).NumeroAutenticacaoPagamento);
        }

        [Fact]
        public async Task PagarBoleto_QuitacaoEmDiaFuturo_FicaAgendado()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao), (HttpStatusCode.OK, RespostaEfetivacao("20261112")));
            var movimento = Boleto();

            await ClienteBoleto(handler).PagarBoletoComConsultaAsync(movimento, Guid.NewGuid());

            Assert.Equal(BancoBrSituacaoEnum.Agendado, movimento.Situacao);
            Assert.Equal(new DateTime(2026, 11, 12), movimento.Horario);
        }

        [Fact]
        public async Task PagarBoleto_JaConsultado_EfetivaSemRepetirConsulta()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RespostaEfetivacao("20261111")));
            var movimento = Boleto();
            var item = (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem;
            item.DataVencimento = new DateTime(2026, 12, 31);
            item.ValorCodigoBarra = 100.99m;
            item.IdentificadorConsulta = "0237CP01251271370358";

            await ClienteBoleto(handler).PagarBoletoAsync(movimento, null);

            Assert.Single(handler.Requests);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ABC")]
        [InlineData("1234567890")]
        [InlineData("0")]
        public async Task PagarBoleto_NumeroDocumentoInvalido_NaoEnvia(string? numeroDocumento)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
            var movimento = Boleto();
            movimento.NumeroDocumento = numeroDocumento;

            await Assert.ThrowsAsync<ArgumentException>(() => ClienteBoleto(handler).PagarBoletoAsync(movimento, null));

            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task PagarBoleto_ErroDoBanco_PreservaCodigoDaCausa()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.PreconditionFailed,
                "{\"status\":412,\"transacao\":\"CBCAIABF\",\"mensagem\":\"Falha na requisição\",\"causa\":\"CBCA0211 - ID da transação do pagamento não encontrado\"}");

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => ClienteBoleto(handler).ConsultarBoletoAsync(Boleto()));

            Assert.Equal(412, ex.HttpStatusCode);
            Assert.Equal("CBCA0211", ex.Mensagens[0].Codigo);
            Assert.Equal("ID da transação do pagamento não encontrado", ex.Mensagens[0].Mensagem);
        }

        [Fact]
        public async Task PagarBoleto_StatusDeErroNoCorpoComHttp200_LancaErro()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":400,\"mensagem\":\"Parâmetros informados inválidos\",\"causa\":\"CBCA0204 - CPF/CNPJ inválido\"}");

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => ClienteBoleto(handler).ConsultarBoletoAsync(Boleto()));

            Assert.Equal("CBCA0204", ex.Mensagens[0].Codigo);
        }

        [Fact]
        public async Task ConsultarBoleto_TokenRecusadoUmaVez_RepeteAChamada()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.Unauthorized, null), (HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao));

            await ClienteBoleto(handler).ConsultarBoletoAsync(Boleto());

            Assert.Equal(3, handler.Requests.Count);
        }

        [Theory]
        [InlineData(1, BancoBrSituacaoEnum.Efetivado)]
        [InlineData(11, BancoBrSituacaoEnum.Efetivado)]
        [InlineData(7, BancoBrSituacaoEnum.Agendado)]
        [InlineData(2, BancoBrSituacaoEnum.Cancelado)]
        [InlineData(3, BancoBrSituacaoEnum.Cancelado)]
        [InlineData(8, BancoBrSituacaoEnum.Cancelado)]
        [InlineData(9, BancoBrSituacaoEnum.Cancelado)]
        [InlineData(12, BancoBrSituacaoEnum.Rejeitado)]
        public async Task ConsultarComprovantePorId_MapeiaSituacaoDoPagamento(int situacao, BancoBrSituacaoEnum esperado)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
                "{\"status\":200,\"situacaoPagamento\":" + situacao + ",\"dataInclTit\":20261111,\"valorPagamento\":93.34,\"horaTransacao\":95256,\"motNaoEfet\":5,\"descrMotivo\":\"SALDO INSUFICIENTE\"," +
                "\"dataVencimento\":20261231,\"isacdo\":\"GOMIDE, DIEGUES E TEL\",\"ccgcCpfSacdo\":62797261414882,\"valorMultaTitulo\":1.00,\"valorJurosTitulo\":0.50}");
            var movimento = Boleto();
            movimento.NumeroDocumentoNoBanco = "22";

            await ClienteBoleto(handler).ConsultarComprovantePorIdAsync(movimento);

            var corpo = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal("001", (string)corpo["versaoLayout"]!);
            Assert.Equal(237, (int)corpo["numeroBanco"]!);
            Assert.Equal(2856, (int)corpo["numeroAgencia"]!);
            Assert.Equal(50035, (int)corpo["numeroConta"]!);
            Assert.Equal(0, (int)corpo["numeroProtocolo"]!);
            Assert.Equal(22, (long)corpo["transactionId"]!);
            Assert.Equal(esperado, movimento.Situacao);
            Assert.Equal(esperado == BancoBrSituacaoEnum.Rejeitado || esperado == BancoBrSituacaoEnum.Cancelado ? "SALDO INSUFICIENTE" : null, movimento.DetalheRejeicao);
            Assert.Equal(93.34m, movimento.ValorPagamento);
            Assert.Equal(new DateTime(2026, 11, 11, 9, 52, 56), movimento.Horario);
            var item = (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem;
            Assert.Equal("62797261414882", item.PagadorConfirmadoDocumento);
            Assert.Equal(1.50m, item.Acrescimo);
        }

        [Fact]
        public async Task CancelarAgendamento_ExcluiPeloTransactionIdEAplicaCancelado()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":200,\"causa\":\"CANCELAMENTO EFETUADO\"}");
            var movimento = Boleto();
            movimento.NumeroDocumentoNoBanco = "22";

            await ClienteBoleto(handler).CancelarAgendamentoAsync(movimento);

            Assert.EndsWith("cobranca-excluir-agendamento/exclusao/executar", handler.LastRequest!.RequestUri!.AbsolutePath);
            var corpo = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal(2, (int)corpo["versao"]!);
            Assert.Equal(22, (long)corpo["idTransacao"]!);
            Assert.Equal(0, (int)corpo["numeroProtocolo"]!);
            Assert.Equal(2856, (int)corpo["agenciaDebito"]!);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, movimento.Situacao);
        }

        [Fact]
        public async Task PagarLoteBoletos_ErroEmUmItem_NaoInterrompeOsDemais()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.PreconditionFailed, "{\"causa\":\"CBCA0211 - ID da transação do pagamento não encontrado\"}"),
                (HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao), (HttpStatusCode.OK, RespostaEfetivacao("20261111")));
            var primeiro = Boleto();
            var segundo = Boleto();
            segundo.NumeroDocumento = "23";

            var resultados = await ClienteBoleto(handler).PagarLoteBoletosAsync(new[] { (primeiro, Guid.NewGuid()), (segundo, Guid.NewGuid()) });

            Assert.Equal(2, resultados.Count);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, segundo.Situacao);
        }

        #endregion

        #region ::. Fachada .::

        private static CertificateSource CertificadoFake()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=fake", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificado = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            return CertificateSource.FromPfxBytes(certificado.Export(X509ContentType.Pfx, "senha"), "senha");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Conectar_Bradesco_RetornaBancoApiEmProducaoEHomologacao(bool homologacao)
        {
            var configuracao = new ConfiguracaoAPI { ClientId = "id", ClientSecret = "segredo", CertificadoDigital = CertificadoFake(), AmbienteHomologacao = homologacao, Correntista = Origem() };

            Assert.NotNull(BancoApi.Conectar(configuracao));
            Assert.NotNull(BancoApi.Conectar(configuracao, Token()));
        }

        [Fact]
        public void Conectar_BradescoSemCertificado_LancaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "id", Correntista = Origem() }));
        }

        [Fact]
        public async Task EnviarAgendamentoAsync_BoletoBradesco_ConsultaEPagaPeloMesmoMetodoPublico()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RespostaValida), (HttpStatusCode.OK, RespostaPreEfetivacao), (HttpStatusCode.OK, RespostaEfetivacao("20261111")));
            var movimento = Boleto();

            var resultado = await Fachada(boleto: ClienteBoleto(handler)).EnviarAgendamentoAsync(movimento);

            Assert.Same(movimento, resultado);
            Assert.Equal(3, handler.Requests.Count);
            Assert.Equal("22", movimento.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task ConsultarAgendamentoAsync_BoletoEnviado_ConsultaPagamentoEspecifico()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":200,\"situacaoPagamento\":1,\"valorPagamento\":10}");
            var movimento = Boleto();
            movimento.NumeroDocumentoNoBanco = "22";

            await Fachada(boleto: ClienteBoleto(handler)).ConsultarAgendamentoAsync(movimento);

            Assert.EndsWith("cobranca-pagamento-consulta/consulta-pagamento-especifico", handler.LastRequest!.RequestUri!.AbsolutePath);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
        }

        [Fact]
        public async Task EnviarAgendamentoAsync_TedEPixBradesco_EnviamSemEtapasExtras()
        {
            var tedHandler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"codigoDeRetorno\":23,\"chaveUnicaParaApi\":\"29694332025-03-06-15.57.10.088927\"}");
            var pixHandler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{\"status\":\"CONCLUIDO\",\"e2e\":\"E60746948202211301715L2856oOXfn8\"}");
            var banco = Fachada(
                pix: new PagamentoPixClient(new HttpClient(pixHandler), Token(), Origem()),
                ted: new PagamentoTedClient(new HttpClient(tedHandler), Token(), Origem()));

            var ted = await banco.EnviarAgendamentoAsync(Ted());
            var pix = await banco.EnviarAgendamentoAsync(Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "a@b.com" }));

            Assert.Equal(BancoBrSituacaoEnum.Agendado, ted.Situacao);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, pix.Situacao);
        }

        [Fact]
        public async Task EnviarAgendamentoAsync_PixJaEnviado_NaoReenvia()
        {
            var pixHandler = new FakeHttpMessageHandler(HttpStatusCode.OK);
            var movimento = Pix(new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "a@b.com" });
            movimento.NumeroDocumentoNoBanco = "E60746948202211301715L2856oOXfn8";

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                Fachada(pix: new PagamentoPixClient(new HttpClient(pixHandler), Token(), Origem())).EnviarAgendamentoAsync(movimento));

            Assert.Null(pixHandler.LastRequest);
        }

        [Fact]
        public async Task ConsultarSaldoAsync_Bradesco_NaoSuportado_SaldoVemDoExtrato()
        {
            var banco = Fachada(boleto: ClienteBoleto(new FakeHttpMessageHandler(HttpStatusCode.OK)));

            await Assert.ThrowsAsync<NotSupportedException>(() => banco.ConsultarSaldoAsync());
        }

        #endregion

        #region ::. Convênio (contas de consumo e tributos) .::

        private const string CodigoConvenio = "84600000000619000820899963269931123031308899";

        private static Movimento Convenio(string? codigo = null, decimal valor = 0, string? numeroDocumento = "TX1") => new Movimento
        {
            NumeroDocumento = numeroDocumento, DataPagamento = new DateTime(2026, 11, 11), ValorPagamento = valor,
            MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra { CodigoBarra = codigo ?? CodigoConvenio }
        };

        private static string RegistroConvenio(string dataDebito = "2024-11-08", string retorno = "000") => Json(@"{
 'retorno': '" + retorno + @"', 'sqlCode': '000', 'banco': 237, 'agencia': 3995, 'conta': 75557,
 'regSaida': [ { 'codigoEmpresaConveniada': 40082, 'documento': '3269931', 'dataDebito': '" + dataDebito + @"', 'valorCodigoBarras': '0000000006190', 'valorDebito': 61.9, 'multa': 1.5, 'juros': 0.5,
   'descontos': 0.0, 'exercicio': 2026, 'autenticacaoBancaria': 45551127, 'codigoBarras': '" + CodigoConvenio + @"', 'dataVencimentoOrgao': '2024-11-08',
   'nomeEmpresaConveniada': 'TELEFONICA BRASIL S/', 'dataPagamento': '2024-11-08', 'horaPagamento': '201039', 'nsuBanco': 0 } ] }");

        private static PagamentoConvenioClient ClienteConvenio(HttpMessageHandler handler) =>
            new PagamentoConvenioClient(new HttpClient(handler), Token(), Origem(), null, () => Agora);

        [Fact]
        public async Task ConsultarCodigoBarras_ValidaComTipoRegistro0EPreencheODocumento()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio());
            var movimento = Convenio();

            await ClienteConvenio(handler).ConsultarCodigoBarrasAsync(movimento);

            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("https://openapi.bradesco.com.br/pagamento/arrecadacao-via-codbarras/v1/pagamentoContaConsumo", handler.LastRequest.RequestUri!.ToString());
            var corpo = JObject.Parse(handler.LastRequestBody!);
            Assert.Equal(2856, (int)corpo["agencia"]!);
            Assert.Equal(0, (int)corpo["digitoAgencia"]!);
            Assert.Equal(50035, (int)corpo["conta"]!);
            Assert.Equal("6", (string)corpo["digitoConta"]!);
            Assert.Equal(CodigoConvenio, (string)corpo["codigoBarras"]!);
            Assert.Equal("2026-11-11", (string)corpo["dataDebito"]!);
            Assert.Equal(0, (int)corpo["tipoRegistro"]!);
            Assert.Equal(1, (int)corpo["tipoConta"]!);
            Assert.Equal("TX1", (string)corpo["idTransacao"]!);
            Assert.Equal(61.90m, (decimal)corpo["valorPrincipal"]!);

            var item = (MovimentoItemPagamentoConvenioCodigoBarra)movimento.MovimentoItem;
            Assert.Equal(61.90m, item.ValorDocumento);
            Assert.Equal(1.5m, item.ValorMulta);
            Assert.Equal(0.5m, item.ValorJuros);
            Assert.Equal("TELEFONICA BRASIL S/", item.Convenio);
            Assert.Equal(new DateTime(2024, 11, 8), item.DataVencimento);
            Assert.Equal(2026, item.AnoExercicio);
            Assert.Equal(61.9m, movimento.ValorPagamento);
            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, movimento.Situacao);
            Assert.Null(movimento.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task ConsultarCodigoBarras_SemNumeroDocumento_ConsultaSemIdTransacao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio());

            await ClienteConvenio(handler).ConsultarCodigoBarrasAsync(Convenio(numeroDocumento: null));

            Assert.Null(JObject.Parse(handler.LastRequestBody!)["idTransacao"]);
        }

        [Fact]
        public async Task PagarConvenio_EnviaTipoRegistro1EGuardaIdTransacao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio());
            var movimento = Convenio(valor: 61.90m);

            await ClienteConvenio(handler).PagarConvenioAsync(movimento);

            Assert.Equal(1, (int)JObject.Parse(handler.LastRequestBody!)["tipoRegistro"]!);
            Assert.Equal("TX1", movimento.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
            var item = (MovimentoItemPagamentoConvenioCodigoBarra)movimento.MovimentoItem;
            Assert.Equal("45551127", item.Autenticacao);
            Assert.Equal(new DateTime(2024, 11, 8, 20, 10, 39), movimento.Horario);
        }

        [Fact]
        public async Task PagarConvenio_DebitoEmDataFutura_FicaAgendado()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio(dataDebito: "2026-11-20"));
            var movimento = Convenio(valor: 61.90m);

            await ClienteConvenio(handler).PagarConvenioAsync(movimento);

            Assert.Equal(BancoBrSituacaoEnum.Agendado, movimento.Situacao);
        }

        [Fact]
        public async Task PagarConvenio_RepresentacaoNumericaDe48Digitos_EnviaCodigoDe44()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio());
            var movimento = Convenio(codigo: "84600000000-6 61900082089-1 99632699311-9 23031308899-2", valor: 61.90m);

            await ClienteConvenio(handler).PagarConvenioAsync(movimento);

            Assert.Equal(CodigoConvenio, (string)JObject.Parse(handler.LastRequestBody!)["codigoBarras"]!);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("COM ESPACO")]
        [InlineData("1234567890")]
        public async Task PagarConvenio_IdTransacaoInvalido_NaoEnvia(string? numeroDocumento)
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);

            await Assert.ThrowsAsync<ArgumentException>(() => ClienteConvenio(handler).PagarConvenioAsync(Convenio(valor: 61.90m, numeroDocumento: numeroDocumento)));

            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task PagarConvenio_CodigoSemValorEValorNaoInformado_NaoEnvia()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);

            // Terceiro dígito 8: o código traz só um valor de referência, então o valor tem de ser informado.
            await Assert.ThrowsAsync<InvalidOperationException>(() => ClienteConvenio(handler).PagarConvenioAsync(Convenio(codigo: "84800000000619000820899963269931123031308899")));

            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task PagarConvenio_RetornoDeRecusaComHttp200_LancaErroComDescricaoDoManual()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio(retorno: "015"));

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => ClienteConvenio(handler).PagarConvenioAsync(Convenio(valor: 61.90m)));

            Assert.Equal("015", ex.Mensagens[0].Codigo);
            Assert.Equal("Saldo insuficiente", ex.Mensagens[0].Mensagem);
        }

        [Fact]
        public async Task ConsultarComprovantePorNsu_ProcuraEfetuadosDepoisAgendados()
        {
            var vazio = "{\"retorno\":\"000\",\"regSaida\":[]}";
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, vazio), (HttpStatusCode.OK, RegistroConvenio(dataDebito: "2026-11-20")));
            var movimento = Convenio();
            movimento.NumeroDocumentoNoBanco = "TX1";

            await ClienteConvenio(handler).ConsultarComprovantePorNsuAsync(movimento);

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.Equal("/pagamento/arrecadacao-via-codbarras/v1/2856/50035/1", handler.Requests[0].RequestUri!.AbsolutePath);
            Assert.Equal("?tipoConsulta=3&segmentoConsulta=99&dataInicial=2026-11-11&dataFinal=2026-11-26&idTransacao=TX1", handler.Requests[0].RequestUri!.Query);
            Assert.Contains("tipoConsulta=4", handler.Requests[1].RequestUri!.Query);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, movimento.Situacao);
        }

        [Theory]
        [InlineData(0, BancoBrSituacaoEnum.Efetivado)]
        [InlineData(2, BancoBrSituacaoEnum.Rejeitado)]
        public async Task ConsultarComprovantePorNsu_MapeiaOSegmentoEncontrado(int vazios, BancoBrSituacaoEnum esperado)
        {
            var vazio = (HttpStatusCode.OK, (string?)"{\"retorno\":\"000\",\"regSaida\":[]}");
            var respostas = Enumerable.Repeat(vazio, vazios).Append((HttpStatusCode.OK, (string?)RegistroConvenio())).ToArray();
            var handler = new SequencedFakeHttpMessageHandler(respostas);
            var movimento = Convenio();
            movimento.NumeroDocumentoNoBanco = "TX1";

            await ClienteConvenio(handler).ConsultarComprovantePorNsuAsync(movimento);

            Assert.Equal(vazios + 1, handler.Requests.Count);
            Assert.Equal(esperado, movimento.Situacao);
            Assert.Equal(esperado == BancoBrSituacaoEnum.Rejeitado ? "Débito não efetuado." : null, movimento.DetalheRejeicao);
        }

        [Fact]
        public async Task ConsultarComprovantePorNsu_NaoEncontrado_FicaNaoIntegradoComUltimoErro()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, "{\"retorno\":\"000\",\"regSaida\":[]}"), (HttpStatusCode.OK, "{\"retorno\":\"000\",\"regSaida\":[]}"), (HttpStatusCode.OK, "{\"retorno\":\"010\"}"));
            var movimento = Convenio();
            movimento.NumeroDocumentoNoBanco = "TX1";

            await ClienteConvenio(handler).ConsultarComprovantePorNsuAsync(movimento);

            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, movimento.Situacao);
            Assert.Equal("Agência / conta não cadastrada", movimento.DetalheRejeicao);
        }

        [Fact]
        public async Task EnviarAgendamentoAsync_ConvenioBradesco_ConsultaEPagaPeloMesmoMetodoPublico()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, RegistroConvenio()), (HttpStatusCode.OK, RegistroConvenio()));
            var movimento = Convenio();

            var resultado = await Fachada(convenio: ClienteConvenio(handler)).EnviarAgendamentoAsync(movimento);

            Assert.Same(movimento, resultado);
            Assert.Equal(new[] { 0, 1 }, handler.Bodies.Select(b => (int)JObject.Parse(b!)["tipoRegistro"]!));
            Assert.Equal("TX1", movimento.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
        }

        [Fact]
        public async Task ConsultarAgendamentoAsync_ConvenioEnviado_ConsultaPeloIdTransacao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, RegistroConvenio());
            var movimento = Convenio();
            movimento.NumeroDocumentoNoBanco = "TX1";

            await Fachada(convenio: ClienteConvenio(handler)).ConsultarAgendamentoAsync(movimento);

            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.Situacao);
        }

        [Fact]
        public async Task ConvenioBradesco_ConsultasEmListaEConciliacao_NaoSuportadas()
        {
            var cliente = ClienteConvenio(new FakeHttpMessageHandler(HttpStatusCode.OK));

            await Assert.ThrowsAsync<NotSupportedException>(() => cliente.ConsultarPagamentosAsync(CodigoConvenio, 237, Agora));
            await Assert.ThrowsAsync<NotSupportedException>(() => cliente.ConsultarConciliacoesAsync(237, Agora));
            await Assert.ThrowsAsync<NotSupportedException>(() => cliente.ConsultarConveniosHabilitadosAsync(1, 237));
        }

        #endregion

        #region ::. Extrato .::

        private const string ExtratoSucesso = @"{
 'extratoUltimosLancamentos': { 'codigoRetorno': '00', 'listaLancamentos': {
   'Saldo Anterior': [ { 'sinalSaldo': '+', 'valorLancamento': '0,00', 'identificacaoSubCodigo': 'S', 'numeroDocumento': '0000000', 'valorSaldoAposLancamento': '9.999,00', 'dataLancamento': '09/11/2026' } ],
   'Ultimos Lancamentos': [ { 'sinalSaldo': '+', 'valorLancamento': '210,00', 'segundaLinhalLancamento': 'TESTE', 'sinalLancamento': '+', 'identificacaoSubCodigo': 'N', 'codigoLancamento': '00998',
      'descritivoLancamentoAbreviado': 'CHQ COMPENSADO', 'numeroDocumento': '0000006', 'valorSaldoAposLancamento': '590,00', 'dataLancamento': '10/11/2026', 'descritivoLancamentoCompleto': 'CHEQUE COMPENSADO' } ],
   'Lancamentos Dia': [ { 'sinalSaldo': '-', 'valorLancamento': '1.500,50', 'segundaLinhalLancamento': '', 'sinalLancamento': '-', 'identificacaoSubCodigo': 'N', 'codigoLancamento': '03212',
      'descritivoLancamentoAbreviado': 'TRANSFE PIX', 'numeroDocumento': '0936269', 'valorSaldoAposLancamento': '910,50', 'dataLancamento': '11/11/2026', 'descritivoLancamentoCompleto': 'TRANSFERENCIA PIX' } ] } },
 'extratoLancamentosFuturos': { 'codigoRetorno': '00', 'listaLancamentos': { 'Lancamentos Futuros': [ { 'sinalLancamento': '-', 'valorLancamento': '99,00', 'valorSaldoAposLancamento': '1,00', 'sinalSaldo': '+', 'dataLancamento': '20/11/2026' } ] } },
 'extratoPorPeriodo': { 'codigoRetorno': '00', 'lstLancamentoMensal': [
   { 'dataLancamento': '31/10/2026', 'numeroDocumento': '0000000', 'valorLancamento': '0,00', 'sinalLancamento': '+', 'valorSaldoAposLancamento': '80,00', 'sinalSaldo': '+', 'identificacaoSubCodigo': null, 'tipoLancamento': '01',
      'descritivoLancamentoAbreviado': 'Saldo Anterior', 'descritivoLancamentoCompleto': 'Saldo Anterior' },
   { 'dataLancamento': '03/11/2026', 'numeroDocumento': '0000001', 'valorLancamento': '700,00', 'sinalLancamento': '-', 'valorSaldoAposLancamento': '620,00', 'sinalSaldo': '-', 'tipoLancamento': '02',
      'codigoLancamento': '03212', 'descritivoLancamentoAbreviado': 'TRANSFE PIX', 'descritivoLancamentoCompleto': 'TRANSFERENCIA PIX' },
   { 'dataLancamento': '05/11/2026', 'numeroDocumento': '0000002', 'valorLancamento': '1.000,00', 'sinalLancamento': '+', 'valorSaldoAposLancamento': '380,00', 'sinalSaldo': '+', 'tipoLancamento': '02',
      'codigoLancamento': '03210', 'descritivoLancamentoAbreviado': 'RECB QRCODE PIX', 'descritivoLancamentoCompleto': 'RECB. QRCODE DINAMIC PIX' },
   { 'dataLancamento': '02/12/2026', 'numeroDocumento': '0000003', 'valorLancamento': '5,00', 'sinalLancamento': '+', 'valorSaldoAposLancamento': '385,00', 'sinalSaldo': '+', 'tipoLancamento': '02' } ] }
}";

        private static ExtratoClient ClienteExtrato(HttpMessageHandler handler) => new ExtratoClient(new HttpClient(handler), Token(), Origem());

        [Fact]
        public async Task ConsultarExtrato_UneBlocosEUsaSaldoAnteriorEPosteriorDoBanco()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, Json(ExtratoSucesso));

            var movimentos = await ClienteExtrato(handler).ConsultarExtratoAsync(999, 11, 2026, null, null, false);

            Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.Equal("https://openapi.bradesco.com.br/v1/fornecimento-extratos-contas/extratos?agencia=2856&conta=50035&tipo=cc&dataInicio=01112026&dataFim=30112026",
                handler.LastRequest.RequestUri!.ToString());

            var saldo = Assert.IsType<MovimentoItemExtratoSaldo>(movimentos[0].MovimentoItem);
            Assert.Equal(80m, saldo.SaldoAnterior);
            Assert.Equal(-910.50m, saldo.SaldoAtual);

            var transacoes = movimentos.Skip(1).Select(m => Assert.IsType<MovimentoItemExtratoTransacao>(m.MovimentoItem)).ToList();
            Assert.Equal(4, transacoes.Count);
            Assert.Equal(new[] { 700m, 1000m, 210m, 1500.50m }, transacoes.Select(t => t.Valor));
            Assert.Equal(new[] { BancoBrTipoCreditoDebitoEnum.Debito, BancoBrTipoCreditoDebitoEnum.Credito, BancoBrTipoCreditoDebitoEnum.Credito, BancoBrTipoCreditoDebitoEnum.Debito },
                transacoes.Select(t => t.Tipo));
            Assert.Equal(new DateTime(2026, 11, 3), transacoes[0].Data);
            Assert.Equal("0000001", transacoes[0].NumeroDocumento);
            Assert.Equal("TRANSFERENCIA PIX", transacoes[0].Descricao);
            Assert.Equal("TESTE", transacoes[2].DescricaoInformacaoComplementar);
            var historico = Assert.Single(transacoes[2].DescricaoComplementarRetorno!);
            Assert.Equal("00998", historico.Codigo);
            Assert.Equal("CHQ COMPENSADO", historico.Descricao);
        }

        [Fact]
        public async Task ConsultarExtrato_SemLancamentos_DevolveSoOSaldoAnterior()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, Json(@"{ 'extratoPorPeriodo': { 'codigoRetorno': '00', 'lstLancamentoMensal': [
                { 'dataLancamento': '31/10/2026', 'valorLancamento': '0,00', 'sinalLancamento': '+', 'valorSaldoAposLancamento': '80,00', 'sinalSaldo': '+', 'descritivoLancamentoAbreviado': 'Saldo Anterior' } ] } }"));

            var movimentos = await ClienteExtrato(handler).ConsultarExtratoAsync(0, 11, 2026, 10, 12, false);

            var saldo = Assert.IsType<MovimentoItemExtratoSaldo>(Assert.Single(movimentos).MovimentoItem);
            Assert.Equal(80m, saldo.SaldoAtual);
            Assert.Contains("dataInicio=10112026&dataFim=12112026", handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task ConsultarExtrato_MaisDadosQueOLimite_FalhaEmVezDeTruncar()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, Json("{ 'extratoPorPeriodo': { 'codigoRetorno': '01', 'lstLancamentoMensal': [] } }"));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ClienteExtrato(handler).ConsultarExtratoAsync(0, 11, 2026, null, null, false));

            Assert.Contains("reduza o período", ex.Message);
        }

        [Fact]
        public async Task ConsultarExtrato_CodigoDeErroComHttp200_LancaErroDoBanco()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, Json("{ 'extratoUltimosLancamentos': { 'codigoRetorno': '12', 'mensagem': 'CTAS2183 - ERRO NAO PREVISTO' } }"));

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => ClienteExtrato(handler).ConsultarExtratoAsync(0, 11, 2026, null, null, false));

            Assert.Equal("12", ex.Mensagens[0].Codigo);
            Assert.Equal("CTAS2183 - ERRO NAO PREVISTO", ex.Mensagens[0].Mensagem);
        }

        [Fact]
        public async Task ConsultarExtrato_ErroHttp400_PreservaCodigoEDescricao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, "{\"codigoErro\":\"PERIODO NAO PERMITIDO\",\"descricao\":\"Intervalo maximo de pesquisa permitido: 1 ano\"}");

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => ClienteExtrato(handler).ConsultarExtratoAsync(0, 11, 2026, null, null, false));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal("PERIODO NAO PERMITIDO", ex.Mensagens[0].Codigo);
            Assert.Equal("Intervalo maximo de pesquisa permitido: 1 ano", ex.Mensagens[0].Mensagem);
        }

        [Fact]
        public async Task ConsultarExtrato_AgruparCnabOuPeriodoInvertido_NaoEnvia()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
            var cliente = ClienteExtrato(handler);

            await Assert.ThrowsAsync<NotSupportedException>(() => cliente.ConsultarExtratoAsync(0, 11, 2026, null, null, true));
            await Assert.ThrowsAsync<ArgumentException>(() => cliente.ConsultarExtratoAsync(0, 11, 2026, 20, 10, false));

            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public async Task ConsultarExtratoAsync_Fachada_UsaContaDoCorrentistaEIgnoraONumeroInformado()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, Json(ExtratoSucesso));

            var movimentos = await Fachada(extrato: ClienteExtrato(handler)).ConsultarExtratoAsync(123456, 11, 2026);

            Assert.Contains("agencia=2856&conta=50035", handler.LastRequest!.RequestUri!.ToString());
            Assert.IsType<MovimentoItemExtratoSaldo>(movimentos[0].MovimentoItem);
        }

        #endregion
    }
}

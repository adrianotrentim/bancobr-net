using System.Net;
using BancoBr.API.Core.Errors;
using BancoBr.API.Santander.Pagamentos.Boletos;
using BancoBr.API.Santander.Pagamentos.Convenios;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.Santander
{
    /// <summary>Boleto (bank_slip_payments) e Convênio (barcode_payments) do Santander: mesmo ciclo POST/PATCH/GET dentro de uma workspace.</summary>
    public class PagamentoCodigoClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/management_payments_partners/v1");

        private const string PaymentId = "11111111-2222-3333-4444-555555555555";
        private const string CodigoBarrasBoleto = "03391234500000150001234567890123456789012345";
        private const string CodigoBarrasConvenio = "82640000001234567890123456789012345678901234";

        private const string BoletoReadyToPay = @"{
            ""id"": ""11111111-2222-3333-4444-555555555555"", ""status"": ""READY_TO_PAY"", ""code"": ""03391234500000150001234567890123456789012345"",
            ""dueDate"": ""2026-10-20"", ""nominalValue"": 150.00, ""deductedValue"": 5.00, ""addedValue"": 2.50, ""totalValue"": 147.50,
            ""payer"": { ""name"": ""Pagador SA"", ""documentType"": ""CNPJ"", ""documentNumber"": ""98765432000100"" },
            ""beneficiary"": { ""name"": ""Fornecedor LTDA"", ""documentType"": ""CNPJ"", ""documentNumber"": ""12345678000100"" }
        }";

        private const string BoletoPago = @"{
            ""id"": ""11111111-2222-3333-4444-555555555555"", ""status"": ""PAYED"", ""paymentValue"": 147.50, ""totalValue"": 147.50,
            ""transaction"": { ""value"": 147.50, ""code"": ""AUTH123"", ""date"": ""2026-10-12T10:30:00Z"" }
        }";

        private const string ConvenioReadyToPay = @"{
            ""id"": ""11111111-2222-3333-4444-555555555555"", ""status"": ""READY_TO_PAY"", ""paymentType"": ""UTILITY"", ""totalValue"": 89.90,
            ""concessionary"": { ""code"": ""0123"", ""name"": ""Companhia de Energia"" }
        }";

        private static PagamentoBoletoClient CriarBoleto(HttpMessageHandler handler, Correntista? origem = null) =>
            new PagamentoBoletoClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, origem ?? CriarOrigem(), "ws-1");

        private static PagamentoConvenioClient CriarConvenio(HttpMessageHandler handler) =>
            new PagamentoConvenioClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem(), "ws-1");

        private static SequencedFakeHttpMessageHandler Sequencia(params (HttpStatusCode, string?)[] operacao) =>
            new SequencedFakeHttpMessageHandler(operacao);

        private static Correntista CriarOrigem() => new Correntista
        {
            NumeroAgencia = 1234, NumeroConta = 5678, DVConta = "9",
            Nome = "Minha Empresa LTDA", TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ, CPF_CNPJ = "11.222.333/0001-81",
        };

        private static Movimento CriarMovimentoBoleto(string? codigoBarras = CodigoBarrasBoleto, string? linhaDigitavel = null) => new Movimento
        {
            MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = codigoBarras, NumeroLinhaDigitavel = linhaDigitavel },
        };

        private static Movimento CriarMovimentoConvenio() => new Movimento
        {
            MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra { CodigoBarra = CodigoBarrasConvenio },
        };

        #region ::. Boleto .::

        [Fact]
        public async Task Boleto_ConsultarBoletoAsync_IniciaPagamentoEMapeiaResposta()
        {
            var handler = Sequencia((HttpStatusCode.Created, BoletoReadyToPay));
            var movimento = CriarMovimentoBoleto();

            var resultado = await CriarBoleto(handler).ConsultarBoletoAsync(movimento);

            Assert.Same(movimento, resultado);
            Assert.Equal(1, handler.Requests.Count);
            var post = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, post.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/bank_slip_payments", post.RequestUri!.ToString());
            Assert.Equal("fake-client-id", post.Headers.GetValues("X-Application-Key").Single());
            Assert.Equal(@"{""code"":""" + CodigoBarrasBoleto + @"""}", handler.Bodies[0]);

            var item = (MovimentoItemPagamentoTituloCodigoBarra)resultado.MovimentoItem;
            Assert.Equal(PaymentId, item.IdentificadorConsulta);
            Assert.Null(resultado.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, resultado.SituacaoBancoBr);
            Assert.Equal(147.50m, resultado.ValorPagamento);
            Assert.Equal(new DateTime(2026, 10, 20), item.DataVencimento);
            Assert.Equal(150.00m, item.ValorCodigoBarra);
            Assert.Equal(5.00m, item.Desconto);
            Assert.Equal(2.50m, item.Acrescimo);
            Assert.Equal("Pagador SA", item.PagadorConfirmadoNome);
            Assert.Equal("98765432000100", item.PagadorConfirmadoDocumento);
            Assert.Equal("Fornecedor LTDA", resultado.Favorecido!.Nome);
            Assert.Equal(TipoInscricaoCPFCNPJEnum.CNPJ, resultado.Favorecido.TipoPessoa);
        }

        [Fact]
        public async Task Boleto_ConsultarBoletoAsync_AceitaLinhaDigitavelEAgendaDataFutura()
        {
            var handler = Sequencia((HttpStatusCode.Created, BoletoReadyToPay));
            var movimento = CriarMovimentoBoleto(codigoBarras: null, linhaDigitavel: "03399.12345 67890.123456 78901.234567 8 90123456789");
            movimento.DataPagamento = DateTime.Today.AddDays(3);

            await CriarBoleto(handler).ConsultarBoletoAsync(movimento);

            var body = handler.Bodies[0]!;
            Assert.Contains(@"""code"":""03399123456789012345678901234567890123456789""", body);
            Assert.Contains($@"""paymentDate"":""{DateTime.Today.AddDays(3):yyyy-MM-dd}""", body);
        }

        [Fact]
        public async Task Boleto_ConsultarBoletoAsync_DataDeHojeNaoAgenda()
        {
            var handler = Sequencia((HttpStatusCode.Created, BoletoReadyToPay));
            var movimento = CriarMovimentoBoleto();
            movimento.DataPagamento = DateTime.Today;

            await CriarBoleto(handler).ConsultarBoletoAsync(movimento);

            Assert.DoesNotContain("paymentDate", handler.Bodies[0]!);
        }

        [Fact]
        public async Task Boleto_ConsultarBoletoAsync_SemCodigo_Lanca()
        {
            var handler = Sequencia();
            var movimento = CriarMovimentoBoleto(codigoBarras: null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarBoleto(handler).ConsultarBoletoAsync(movimento));
        }

        [Fact]
        public async Task Boleto_PagarBoletoAsync_EfetivaComPagadorFinalEContaDaWorkspace()
        {
            var handler = Sequencia((HttpStatusCode.OK, BoletoPago));
            var movimento = CriarMovimentoBoleto();
            movimento.ValorPagamento = 147.50m;
            ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).IdentificadorConsulta = PaymentId;

            var resultado = await CriarBoleto(handler).PagarBoletoAsync(movimento, idempotencyKey: "ignorada");

            var patch = handler.Requests[0];
            Assert.Equal("PATCH", patch.Method.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/bank_slip_payments/{PaymentId}", patch.RequestUri!.ToString());
            var body = handler.Bodies[0]!;
            Assert.Contains(@"""paymentValue"":147.5", body);
            Assert.Contains(@"""status"":""AUTHORIZED""", body);
            Assert.Contains(@"""debitAccount"":{""branch"":""1234"",""number"":""000000056789""}", body);
            Assert.Contains(@"""finalPayer"":{""name"":""Minha Empresa LTDA"",""documentType"":""CNPJ"",""documentNumber"":""11222333000181""}", body);

            Assert.Equal(PaymentId, resultado.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal(new DateTime(2026, 10, 12, 10, 30, 0, DateTimeKind.Utc), resultado.HorarioBancoBr!.Value.ToUniversalTime());
            Assert.Equal("AUTH123", ((MovimentoItemPagamentoTituloCodigoBarra)resultado.MovimentoItem).NumeroAutenticacaoPagamento);
        }

        [Fact]
        public async Task Boleto_PagarBoletoAsync_SemConsultaPrevia_Lanca()
        {
            var movimento = CriarMovimentoBoleto();
            movimento.ValorPagamento = 10m;

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarBoleto(Sequencia()).PagarBoletoAsync(movimento, idempotencyKey: null));
        }

        [Fact]
        public async Task Boleto_PagarBoletoAsync_SemDadosDoTitular_Lanca()
        {
            var movimento = CriarMovimentoBoleto();
            movimento.ValorPagamento = 10m;
            ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).IdentificadorConsulta = PaymentId;
            var origem = CriarOrigem();
            origem.Nome = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarBoleto(Sequencia(), origem).PagarBoletoAsync(movimento, idempotencyKey: null));
        }

        [Fact]
        public async Task Boleto_PagarBoletoComConsultaAsync_IniciaEEfetiva()
        {
            var handler = Sequencia((HttpStatusCode.Created, BoletoReadyToPay), (HttpStatusCode.OK, BoletoPago));
            var movimento = CriarMovimentoBoleto();

            var resultado = await CriarBoleto(handler).PagarBoletoComConsultaAsync(movimento, Guid.NewGuid());

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.Equal("PATCH", handler.Requests[1].Method.Method);
            Assert.Contains(@"""paymentValue"":147.5", handler.Bodies[1]!);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal(PaymentId, resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task Boleto_PagarBoletoComConsultaAsync_Rejeitado_NaoEfetiva()
        {
            var handler = Sequencia((HttpStatusCode.Created, @"{ ""id"": """ + PaymentId + @""", ""status"": ""REJECTED"", ""rejectReason"": ""Boleto vencido"" }"));
            var movimento = CriarMovimentoBoleto();

            var resultado = await CriarBoleto(handler).PagarBoletoComConsultaAsync(movimento, Guid.NewGuid());

            Assert.Equal(1, handler.Requests.Count);
            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, resultado.SituacaoBancoBr);
            Assert.Equal("Boleto vencido", resultado.DetalheRejeicaoBancoBr);
        }

        [Fact]
        public async Task Boleto_ConsultarComprovantePorIdAsync_ConsultaPeloId()
        {
            var handler = Sequencia((HttpStatusCode.OK, BoletoPago));
            var movimento = CriarMovimentoBoleto();
            movimento.NumeroDocumentoNoBanco = PaymentId;

            var resultado = await CriarBoleto(handler).ConsultarComprovantePorIdAsync(movimento);

            var get = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, get.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/bank_slip_payments/{PaymentId}", get.RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
        }

        [Theory]
        [InlineData("PENDING_CONFIRMATION", BancoBrSituacaoEnum.Agendado)]
        [InlineData("REFUNDED", BancoBrSituacaoEnum.Cancelado)]
        [InlineData("REJECTED", BancoBrSituacaoEnum.Rejeitado)]
        [InlineData("QUALQUER_COISA", BancoBrSituacaoEnum.NaoIntegrado)]
        public async Task Boleto_MapeiaStatus(string status, BancoBrSituacaoEnum esperado)
        {
            var handler = Sequencia((HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": """ + status + @""" }"));
            var movimento = CriarMovimentoBoleto();
            movimento.NumeroDocumentoNoBanco = PaymentId;

            var resultado = await CriarBoleto(handler).ConsultarComprovantePorIdAsync(movimento);

            Assert.Equal(esperado, resultado.SituacaoBancoBr);
        }

        [Fact]
        public async Task Boleto_ErroDoBanco_ViraBancoApiException()
        {
            var handler = Sequencia((HttpStatusCode.UnprocessableEntity, @"{ ""_errorCode"": ""422"", ""_message"": ""Boleto invalido"", ""_details"": ""Boleto ja pago"" }"));

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => CriarBoleto(handler).ConsultarBoletoAsync(CriarMovimentoBoleto()));

            Assert.Equal(422, ex.HttpStatusCode);
            Assert.Contains(ex.Mensagens, m => m.Mensagem == "Boleto ja pago");
        }

        [Fact]
        public async Task Boleto_SemIdWorkspace_Lanca()
        {
            var client = new PagamentoBoletoClient(new HttpClient(Sequencia()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConsultarBoletoAsync(CriarMovimentoBoleto()));
            Assert.Contains("idWorkspace", ex.Message);
        }

        [Fact]
        public async Task Boleto_OperacoesSemEndpointNaoSaoSuportadas()
        {
            var client = CriarBoleto(Sequencia());

            await Assert.ThrowsAsync<NotSupportedException>(() => client.CancelarAgendamentoAsync(CriarMovimentoBoleto()));
            await Assert.ThrowsAsync<NotSupportedException>(() => client.ConsultarComprovantePorIdempotencyAsync("chave"));
        }

        private static string PaginaDda(int quantidade) => @"{ ""_limit"": 200, ""_pageNumber"": 1, ""_pageElements"": " + quantidade + @", ""_content"": [" +
            string.Join(",", Enumerable.Range(0, quantidade).Select(_ => @"{
                ""code"": ""03391234500000150001234567890123456789012345"", ""authorizedDirectDebitId"": ""987654321"", ""bankIdentificationNumber"": ""1234567890123"",
                ""kindCode"": ""2"", ""dueDate"": ""2026-10-20"", ""paymentLimitDate"": ""2026-11-20"", ""nominalValue"": 150.00, ""payerDocumentNumber"": ""11222333000181"",
                ""beneficiaryIdentificationNumber"": ""NF-123"",
                ""beneficiary"": { ""name"": ""Fornecedor LTDA"", ""documentType"": ""CNPJ"", ""documentNumber"": ""12345678000100"" }
            }")) + "]}";

        [Fact]
        public async Task Boleto_ConsultarBoletosDdaAsync_ConsultaPorVencimentoEMapeiaItens()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, PaginaDda(2)));

            var resultado = await CriarBoleto(handler).ConsultarBoletosDdaAsync(
                56789, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
                BancoBr.API.Base.Models.SituacaoBoletoEnum.EmAberto, BancoBr.API.Base.Models.TipoDataConsultaEnum.Vencimento);

            Assert.Equal(1, handler.Requests.Count);
            var get = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, get.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/authorized_direct_debits?_limit=200&_PageNumber=1&initialDueDate=2026-10-01&finalDueDate=2026-10-31&titleSituation=ALL", get.RequestUri!.ToString());

            Assert.Equal(2, resultado.Count);
            var item = Assert.IsType<MovimentoItemDDA>(resultado[0].MovimentoItem);
            Assert.Equal("03391234500000150001234567890123456789012345", item.NumeroCodigoBarras);
            Assert.Equal(150.00m, item.ValorBoleto);
            Assert.Equal(new DateTime(2026, 10, 20), item.DataVencimentoBoleto);
            Assert.Equal("2026-11-20", item.DataLimitePagamento);
            Assert.Equal("11222333000181", item.NumeroCpfCnpjPagador);
            Assert.Equal("1234567890123", item.NumeroNossoNumero);
            Assert.Equal("NF-123", item.NumeroDocumento);
            Assert.Equal(2, item.CodigoEspecieDocumento);
            Assert.Equal(987654321, item.NumeroIdentificadorBoletoCip);
            Assert.Equal("Fornecedor LTDA", item.NomeRazaoSocialBeneficiario);
            Assert.Equal("CNPJ", item.TipoPessoaBeneficiario);
            Assert.Equal("12345678000100", item.NumeroCpfCnpjBeneficiario);
        }

        [Fact]
        public async Task Boleto_ConsultarBoletosDdaAsync_PaginaAteUmaPaginaIncompleta()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, PaginaDda(200)), (HttpStatusCode.OK, PaginaDda(3)));

            var resultado = await CriarBoleto(handler).ConsultarBoletosDdaAsync(
                56789, new DateTime(2026, 10, 1), new DateTime(2026, 10, 31),
                BancoBr.API.Base.Models.SituacaoBoletoEnum.Liquidado, BancoBr.API.Base.Models.TipoDataConsultaEnum.Vencimento);

            Assert.Equal(203, resultado.Count);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains("_PageNumber=2", handler.Requests[1].RequestUri!.Query);
            Assert.Contains("titleSituation=PAID", handler.Requests[1].RequestUri!.Query);
        }

        [Fact]
        public async Task Boleto_ConsultarBoletosDdaAsync_TipoDataDiferenteDeVencimento_Lanca()
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => new PagamentoBoletoClient(new HttpClient(Sequencia()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, null!, "ws-1").ConsultarBoletosDdaAsync(
                56789, DateTime.Today, DateTime.Today,
                BancoBr.API.Base.Models.SituacaoBoletoEnum.EmAberto, BancoBr.API.Base.Models.TipoDataConsultaEnum.Emissao));
        }

        [Fact]
        public async Task Boleto_ConsultarBoletosDdaAsync_SemOrigem_Lanca()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => new PagamentoBoletoClient(new HttpClient(Sequencia()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, null!, "ws-1").ConsultarBoletosDdaAsync(
                56789, DateTime.Today, DateTime.Today,
                BancoBr.API.Base.Models.SituacaoBoletoEnum.EmAberto, BancoBr.API.Base.Models.TipoDataConsultaEnum.Vencimento));
        }

        #endregion

        #region ::. Convênio .::

        [Fact]
        public async Task Convenio_ConsultarCodigoBarrasAsync_IniciaPagamentoEMapeiaConvenio()
        {
            var handler = Sequencia((HttpStatusCode.Created, ConvenioReadyToPay));
            var movimento = CriarMovimentoConvenio();

            var resultado = await CriarConvenio(handler).ConsultarCodigoBarrasAsync(movimento);

            var post = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, post.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/barcode_payments", post.RequestUri!.ToString());
            Assert.Equal(@"{""code"":""" + CodigoBarrasConvenio + @"""}", handler.Bodies[0]);

            Assert.Equal(PaymentId, resultado.NumeroDocumentoNoBanco);
            Assert.Equal(89.90m, resultado.ValorPagamento);
            var item = (MovimentoItemPagamentoConvenioCodigoBarra)resultado.MovimentoItem;
            Assert.Equal(89.90m, item.ValorDocumento);
            Assert.Equal("Companhia de Energia", item.Convenio);
            Assert.Equal("0123", item.CodigoConvenioFebraban);
        }

        [Fact]
        public async Task Convenio_PagarConvenioAsync_EfetivaComPagadorFinal()
        {
            var handler = Sequencia((HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": ""PAYED"", ""paymentValue"": 89.90, ""transaction"": { ""code"": ""AUTH9"" } }"));
            var movimento = CriarMovimentoConvenio();
            movimento.NumeroDocumentoNoBanco = PaymentId;
            movimento.ValorPagamento = 89.90m;

            var resultado = await CriarConvenio(handler).PagarConvenioAsync(movimento);

            var patch = handler.Requests[0];
            Assert.Equal("PATCH", patch.Method.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/barcode_payments/{PaymentId}", patch.RequestUri!.ToString());
            Assert.Contains(@"""finalPayer"":{""name"":""Minha Empresa LTDA"",""documentType"":""CNPJ"",""documentNumber"":""11222333000181""}", handler.Bodies[0]!);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal("AUTH9", ((MovimentoItemPagamentoConvenioCodigoBarra)resultado.MovimentoItem).Autenticacao);
        }

        [Fact]
        public async Task Convenio_PagarConvenioAsync_SemConsultaPrevia_Lanca()
        {
            var movimento = CriarMovimentoConvenio();
            movimento.ValorPagamento = 10m;

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarConvenio(Sequencia()).PagarConvenioAsync(movimento));
        }

        [Fact]
        public async Task Convenio_ConsultarComprovantePorNsuAsync_ConsultaPeloId()
        {
            var handler = Sequencia((HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": ""PENDING_CONFIRMATION"" }"));
            var movimento = CriarMovimentoConvenio();
            movimento.NumeroDocumentoNoBanco = PaymentId;

            var resultado = await CriarConvenio(handler).ConsultarComprovantePorNsuAsync(movimento);

            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/barcode_payments/{PaymentId}", handler.Requests[0].RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Agendado, resultado.SituacaoBancoBr);
        }

        [Fact]
        public async Task Convenio_SemIdWorkspace_Lanca()
        {
            var client = new PagamentoConvenioClient(new HttpClient(Sequencia()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConsultarCodigoBarrasAsync(CriarMovimentoConvenio()));
            Assert.Contains("idWorkspace", ex.Message);
        }

        [Fact]
        public async Task Convenio_OperacoesSemEndpointNaoSaoSuportadas()
        {
            var client = CriarConvenio(Sequencia());

            await Assert.ThrowsAsync<NotSupportedException>(() => client.ConsultarPagamentosAsync(CodigoBarrasConvenio, 1, DateTime.Today));
            await Assert.ThrowsAsync<NotSupportedException>(() => client.ConsultarConciliacoesAsync(1, DateTime.Today));
            await Assert.ThrowsAsync<NotSupportedException>(() => client.ConsultarConveniosHabilitadosAsync(1, 1));
        }

        #endregion
    }
}

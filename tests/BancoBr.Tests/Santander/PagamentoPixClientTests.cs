using System.Net;
using BancoBr.API.Core.Errors;
using BancoBr.API.Santander.Pagamentos.Pix;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.Santander
{
    public class PagamentoPixClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/management_payments_partners/v1");

        private const string PaymentId = "11111111-2222-3333-4444-555555555555";

        private const string ReadyToPay = @"{
            ""id"": ""11111111-2222-3333-4444-555555555555"", ""status"": ""READY_TO_PAY"",
            ""dictCodeType"": ""EMAIL"", ""dictCode"": ""fornecedor@dominio.com.br"", ""paymentValue"": 150.00,
            ""beneficiary"": { ""name"": ""Fornecedor LTDA"", ""documentType"": ""CNPJ"", ""documentNumber"": ""12345678000100"" }
        }";

        private static PagamentoPixClient CriarClient(HttpMessageHandler handler) =>
            new PagamentoPixClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem(), "ws-1");

        private static SequencedFakeHttpMessageHandler Sequencia(params (HttpStatusCode, string?)[] operacao) =>
            new SequencedFakeHttpMessageHandler(operacao);

        private static Correntista CriarOrigem() => new Correntista { NumeroAgencia = 1234, NumeroConta = 5678, DVConta = "9" };

        private static Movimento CriarMovimentoChave() => new Movimento
        {
            ValorPagamento = 150.00m,
            NumeroDocumento = "NF-10",
            MovimentoItem = new MovimentoItemTransferenciaPIX { TipoChavePIX = FormaIniciacaoEnum.PIX_Email, ChavePIX = "fornecedor@dominio.com.br" },
        };

        [Fact]
        public async Task IniciarPagamentoAsync_Chave_EnviaDictEMapeiaTitular()
        {
            var handler = Sequencia((HttpStatusCode.Created, ReadyToPay));
            var movimento = CriarMovimentoChave();

            var resultado = await CriarClient(handler).IniciarPagamentoAsync(movimento);

            Assert.Same(movimento, resultado);
            Assert.Equal(1, handler.Requests.Count);
            var post = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, post.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/pix_payments", post.RequestUri!.ToString());
            Assert.Equal("fake-client-id", post.Headers.GetValues("X-Application-Key").Single());
            Assert.Equal("Bearer", post.Headers.Authorization!.Scheme);
            var body = handler.Bodies[0]!;
            Assert.Contains(@"""dictCodeType"":""EMAIL""", body);
            Assert.Contains(@"""dictCode"":""fornecedor@dominio.com.br""", body);
            Assert.Contains(@"""paymentValue"":150", body);
            Assert.DoesNotContain("qrCode", body);

            Assert.Equal(PaymentId, resultado.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, resultado.SituacaoBancoBr);
            var item = (MovimentoItemTransferenciaPIX)resultado.MovimentoItem;
            Assert.Equal("Fornecedor LTDA", item.TitularConfirmadoNome);
            Assert.Equal("12345678000100", item.TitularConfirmadoDocumento);
            Assert.Equal("CNPJ", item.TitularConfirmadoTipo);
            Assert.Equal("EMAIL", item.TipoChaveConfirmado);
        }

        [Theory]
        [InlineData(FormaIniciacaoEnum.PIX_Telefone, "+5511999998888", "CELULAR")]
        [InlineData(FormaIniciacaoEnum.PIX_Aleatorio, "123e4567-e89b-12d3-a456-426614174000", "EVP")]
        [InlineData(FormaIniciacaoEnum.PIX_CPF_CNPJ, "123.456.789-00", "CPF")]
        [InlineData(FormaIniciacaoEnum.PIX_CPF_CNPJ, "12.345.678/0001-00", "CNPJ")]
        public async Task IniciarPagamentoAsync_MapeiaTipoDaChave(FormaIniciacaoEnum forma, string chave, string esperado)
        {
            var handler = Sequencia((HttpStatusCode.Created, ReadyToPay));
            var movimento = new Movimento { ValorPagamento = 1m, MovimentoItem = new MovimentoItemTransferenciaPIX { TipoChavePIX = forma, ChavePIX = chave } };

            await CriarClient(handler).IniciarPagamentoAsync(movimento);

            Assert.Contains($@"""dictCodeType"":""{esperado}""", handler.Bodies[0]!);
        }

        private static Movimento CriarMovimentoConta(Action<MovimentoItemTransferenciaPIX>? ajustar = null)
        {
            var item = new MovimentoItemTransferenciaPIX { Banco = 341, NumeroAgencia = 528, NumeroConta = 54321, DVConta = "8", TipoConta = TipoContaEnum.ContaPoupanca };
            ajustar?.Invoke(item);
            return new Movimento
            {
                ValorPagamento = 150.00m,
                Favorecido = new Favorecido { TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF, CPF_CNPJ = "123.456.789-00", Nome = "Fornecedor A" },
                MovimentoItem = item,
            };
        }

        [Fact]
        public async Task IniciarPagamentoAsync_AgenciaEConta_EnviaBeneficiario()
        {
            var handler = Sequencia((HttpStatusCode.Created, @"{ ""id"": """ + PaymentId + @""", ""status"": ""READY_TO_PAY"", ""beneficiary"": { ""name"": ""Fornecedor A"", ""documentType"": ""CPF"", ""documentNumber"": ""12345678900"" } }"));
            var movimento = CriarMovimentoConta();

            var resultado = await CriarClient(handler).IniciarPagamentoAsync(movimento);

            var body = handler.Bodies[0]!;
            Assert.Contains(@"""beneficiary"":{""name"":""Fornecedor A"",""documentType"":""CPF"",""documentNumber"":""12345678900"",""bankCode"":""341"",""branch"":""0528"",""number"":""543218"",""type"":""CONTA_POUPANCA""}", body);
            Assert.DoesNotContain("ispb", body);
            Assert.DoesNotContain("dictCode", body);
            Assert.Equal(PaymentId, resultado.NumeroDocumentoNoBanco);
            Assert.Equal("Fornecedor A", ((MovimentoItemTransferenciaPIX)resultado.MovimentoItem).TitularConfirmadoNome);
        }

        [Fact]
        public async Task IniciarPagamentoAsync_AgenciaEConta_PorIspbEContaDePagamento()
        {
            var handler = Sequencia((HttpStatusCode.Created, ReadyToPay));
            var movimento = CriarMovimentoConta(i => { i.Banco = 0; i.Ispb = "18236120"; i.TipoConta = TipoContaEnum.ContaPagamento; });
            movimento.Favorecido!.TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ;
            movimento.Favorecido.CPF_CNPJ = "12.345.678/0001-00";

            await CriarClient(handler).IniciarPagamentoAsync(movimento);

            var body = handler.Bodies[0]!;
            Assert.Contains(@"""ispb"":""18236120""", body);
            Assert.DoesNotContain("bankCode", body);
            Assert.Contains(@"""documentType"":""CNPJ"",""documentNumber"":""12345678000100""", body);
            Assert.Contains(@"""type"":""CONTA_PAGAMENTO""", body);
        }

        [Fact]
        public async Task IniciarPagamentoAsync_AgenciaEConta_BancoEIspbJuntos_Lanca()
        {
            var client = CriarClient(Sequencia());
            var movimento = CriarMovimentoConta(i => i.Ispb = "18236120");

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.IniciarPagamentoAsync(movimento));
        }

        [Fact]
        public async Task IniciarPagamentoAsync_AgenciaEConta_SemBancoNemIspb_Lanca()
        {
            var client = CriarClient(Sequencia());
            var movimento = CriarMovimentoConta(i => i.Banco = 0);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.IniciarPagamentoAsync(movimento));
        }

        [Fact]
        public async Task IniciarPagamentoAsync_AgenciaEConta_SemFavorecido_Lanca()
        {
            var client = CriarClient(Sequencia());
            var movimento = CriarMovimentoConta();
            movimento.Favorecido = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.IniciarPagamentoAsync(movimento));
        }

        [Fact]
        public async Task IniciarPagamentoAsync_SemChaveNemConta_Lanca()
        {
            var client = CriarClient(Sequencia());
            var movimento = new Movimento { ValorPagamento = 1m, MovimentoItem = new MovimentoItemTransferenciaPIX() };

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.IniciarPagamentoAsync(movimento));
        }

        [Fact]
        public async Task PagarComIniciacaoAsync_PostEPatch_Efetiva()
        {
            var handler = Sequencia(
                (HttpStatusCode.Created, ReadyToPay),
                (HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": ""PAYED"", ""paymentValue"": 150.00, ""transaction"": { ""date"": ""2026-10-09T10:15:00Z"", ""endToEnd"": ""E123"" } }"));
            var movimento = CriarMovimentoChave();

            var resultado = await CriarClient(handler).PagarComIniciacaoAsync(movimento);

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("PATCH", handler.Requests[1].Method.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/pix_payments/{PaymentId}", handler.Requests[1].RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.NotNull(resultado.HorarioBancoBr);
            // o PATCH não devolve o titular de novo — o que veio do POST é mantido
            Assert.Equal("Fornecedor LTDA", ((MovimentoItemTransferenciaPIX)resultado.MovimentoItem).TitularConfirmadoNome);
        }

        [Fact]
        public async Task ConfirmarPagamentoAsync_EnviaAuthorizedEContaDeDebito()
        {
            var handler = Sequencia((HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": ""PENDING_CONFIRMATION"" }"));
            var movimento = CriarMovimentoChave();
            movimento.NumeroDocumentoNoBanco = PaymentId;

            var resultado = await CriarClient(handler).ConfirmarPagamentoAsync(movimento);

            var body = handler.Bodies[0]!;
            Assert.Contains(@"""status"":""AUTHORIZED""", body);
            Assert.Contains(@"""paymentValue"":150", body);
            Assert.Contains(@"""debitAccount"":{""branch"":""1234"",""number"":""000000056789""}", body);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, resultado.SituacaoBancoBr);
        }

        [Fact]
        public async Task PagarComIniciacaoAsync_PostRejeitado_NaoEnviaPatch()
        {
            var handler = Sequencia(
                (HttpStatusCode.Created, @"{ ""id"": """ + PaymentId + @""", ""status"": ""REJECTED"", ""rejectReason"": ""Chave inexistente"" }"));

            var resultado = await CriarClient(handler).PagarComIniciacaoAsync(CriarMovimentoChave());

            Assert.Equal(1, handler.Requests.Count);
            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, resultado.SituacaoBancoBr);
            Assert.Equal("Chave inexistente", resultado.DetalheRejeicaoBancoBr);
        }

        [Fact]
        public async Task ConsultarPagamentoAsync_Get_MapeiaSituacao()
        {
            var handler = Sequencia((HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": ""PAYED"" }"));
            var movimento = CriarMovimentoChave();
            movimento.NumeroDocumentoNoBanco = PaymentId;

            var resultado = await CriarClient(handler).ConsultarPagamentoAsync(movimento);

            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.EndsWith($"/workspaces/ws-1/pix_payments/{PaymentId}", handler.Requests[0].RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
        }


        [Fact]
        public async Task PagarViaQrCodeAsync_PostEPatch_MapeiaQrCode()
        {
            var handler = Sequencia(
                (HttpStatusCode.Created, @"{ ""id"": """ + PaymentId + @""", ""status"": ""READY_TO_PAY"", ""qrCodeIdentification"": ""TX123"", ""nominalValue"": 100.00, ""paymentValue"": 100.00 }"),
                (HttpStatusCode.OK, @"{ ""id"": """ + PaymentId + @""", ""status"": ""PAYED"", ""paymentValue"": 100.00 }"));
            var movimento = new Movimento { ValorPagamento = 100m, MovimentoItem = new MovimentoItemPagamentoTituloPIXQRCode { QrCode = "00020126..." } };

            var resultado = await CriarClient(handler).PagarViaQrCodeAsync(movimento);

            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains(@"""qrCode"":""00020126...""", handler.Bodies[0]!);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            var item = (MovimentoItemPagamentoTituloPIXQRCode)resultado.MovimentoItem;
            Assert.Equal("TX123", item.TXID);
            Assert.Equal(100m, item.ValorOriginal);
        }

        [Fact]
        public async Task SemIdWorkspace_Lanca()
        {
            var client = new PagamentoPixClient(new HttpClient(Sequencia()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.IniciarPagamentoAsync(CriarMovimentoChave()));
            Assert.Contains("idWorkspace", ex.Message);
        }

        [Fact]
        public async Task SemOrigem_Lanca()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");

            await Assert.ThrowsAsync<ArgumentNullException>(() => new PagamentoPixClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, null!, "ws-1").IniciarPagamentoAsync(CriarMovimentoChave()));
        }

        [Fact]
        public async Task Erro400_LancaBancoApiException()
        {
            var handler = Sequencia((HttpStatusCode.BadRequest, @"{
                ""_errorCode"": ""400"", ""_message"": ""Bad request"", ""_details"": ""Dados inválidos"", ""_traceId"": ""abc"",
                ""_errors"": [ { ""_code"": ""E1"", ""_field"": ""paymentValue"", ""_message"": ""deve ser maior que zero"" } ] }"));

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => CriarClient(handler).IniciarPagamentoAsync(CriarMovimentoChave()));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal("Dados inválidos", ex.Mensagens[0].Mensagem);
            Assert.Equal("paymentValue: deve ser maior que zero", ex.Mensagens[1].Mensagem);
        }
    }
}

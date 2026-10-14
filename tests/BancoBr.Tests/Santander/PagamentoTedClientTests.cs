using System.Net;
using BancoBr.API.Santander.Pagamentos.Ted;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.Santander
{
    /// <summary>TED (transfer) do Santander: POST inicia, PATCH efetiva e GET consulta, dentro de uma workspace.</summary>
    public class PagamentoTedClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/management_payments_partners/v1");

        private const string TransferId = "11111111-2222-3333-4444-555555555555";

        private static string Resposta(string status, string extras = "") => $@"{{
            ""id"": ""{TransferId}"", ""status"": ""{status}"", ""transferValue"": 2500.65,
            ""paymentDate"": ""2026-10-12"",
            ""destinationAccount"": {{ ""bankCode"": ""341"", ""branchCode"": ""0528"", ""accountNumber"": ""543218"" }}{extras}
        }}";

        private static PagamentoTedClient CriarClient(HttpMessageHandler handler) =>
            new PagamentoTedClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, "ws-1");

        private static SequencedFakeHttpMessageHandler Sequencia(params (HttpStatusCode, string?)[] operacao) =>
            new SequencedFakeHttpMessageHandler(operacao);

        private static Correntista CriarOrigem() => new Correntista { NumeroAgencia = 1234, NumeroConta = 5678, DVConta = "9" };

        private static Movimento CriarMovimento(Action<MovimentoItemTransferenciaTED>? ajustar = null)
        {
            var item = new MovimentoItemTransferenciaTED
            {
                CodigoFinalidadeTED = FinalidadeTEDEnum.PagamentoImpostosTributosTaxas,
                Banco = 341,
                NumeroAgencia = 528,
                NumeroConta = 54321,
                DVConta = "8",
                TipoConta = TipoContaEnum.ContaCorrente,
                Historico = "Pagamento NF 10",
            };
            ajustar?.Invoke(item);
            return new Movimento
            {
                ValorPagamento = 2500.65m,
                Favorecido = new Favorecido { TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF, CPF_CNPJ = "123.456.789-00", Nome = "Fornecedor A" },
                MovimentoItem = item,
            };
        }

        [Fact]
        public async Task PagarTedAsync_IniciaEConfirma()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("READY_TO_PAY")), (HttpStatusCode.OK, Resposta("PENDING_CONFIRMATION")));
            var movimento = CriarMovimento();

            var resultado = await CriarClient(handler).PagarTedAsync(movimento, CriarOrigem(), Guid.NewGuid().ToString());

            Assert.Same(movimento, resultado);
            Assert.Equal(2, handler.Requests.Count);

            var post = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, post.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/transfer", post.RequestUri!.ToString());
            Assert.Equal("fake-client-id", post.Headers.GetValues("X-Application-Key").Single());
            var corpo = handler.Bodies[0]!;
            Assert.Contains(@"""sourceAccount"":{""branchCode"":""1234"",""accountNumber"":""000000056789""}", corpo);
            Assert.Contains(@"""bankCode"":""341""", corpo);
            Assert.Contains(@"""branchCode"":""0528""", corpo);
            Assert.Contains(@"""accountNumber"":""543218""", corpo);
            Assert.Contains(@"""typeAccount"":""CC""", corpo);
            Assert.Contains(@"""legalEntityIdentifier"":""CPF""", corpo);
            Assert.Contains(@"""documentIdentifierNumber"":""12345678900""", corpo);
            Assert.Contains(@"""name"":""Fornecedor A""", corpo);
            Assert.Contains(@"""purpose"":""PAGAMENTO_DE_IMPOSTOS_TRIBUTOS_E_TAXAS""", corpo);
            Assert.Contains(@"""transferHistory"":""Pagamento NF 10""", corpo);
            Assert.Contains(@"""transferValue"":2500.65", corpo);
            Assert.Contains(@"""destinationType"":""STR0008""", corpo);
            Assert.DoesNotContain("ispbCode", corpo);

            var patch = handler.Requests[1];
            Assert.Equal("PATCH", patch.Method.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/transfer/{TransferId}", patch.RequestUri!.ToString());
            Assert.Contains(@"""status"":""AUTHORIZED""", handler.Bodies[1]!);
            Assert.Contains(@"""sourceAccount"":{""branchCode"":""1234"",""accountNumber"":""000000056789""}", handler.Bodies[1]!);

            Assert.Equal(TransferId, resultado.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, resultado.SituacaoBancoBr);
            var item = (MovimentoItemTransferenciaTED)resultado.MovimentoItem;
            Assert.Equal(TransferId, item.NumeroControleIF);
            Assert.Equal("PENDING_CONFIRMATION", item.Situacao);
            Assert.Equal(new DateTime(2026, 10, 12), resultado.DataPagamento);
        }

        [Fact]
        public async Task PagarTedAsync_ChaveGuid_ViraIdDoPagamento()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("READY_TO_PAY")), (HttpStatusCode.OK, Resposta("PENDING_CONFIRMATION")));

            await CriarClient(handler).PagarTedAsync(CriarMovimento(), CriarOrigem(), "92446ad6-d52a-4141-bee4-58a019358faa");

            Assert.Contains(@"""id"":""92446ad6-d52a-4141-bee4-58a019358faa""", handler.Bodies[0]!);
        }

        [Fact]
        public async Task PagarTedAsync_ChaveQueNaoEGuid_NaoEnviaId()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("READY_TO_PAY")), (HttpStatusCode.OK, Resposta("PENDING_CONFIRMATION")));

            await CriarClient(handler).PagarTedAsync(CriarMovimento(), CriarOrigem(), "chave-qualquer");

            Assert.DoesNotContain(@"""id""", handler.Bodies[0]!);
        }

        [Fact]
        public async Task PagarTedAsync_ContaDePagamento_EnviaIspb()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("READY_TO_PAY")), (HttpStatusCode.OK, Resposta("PENDING_CONFIRMATION")));
            var movimento = CriarMovimento(i => { i.Banco = 260; i.TipoConta = TipoContaEnum.ContaPagamento; });

            await CriarClient(handler).PagarTedAsync(movimento, CriarOrigem(), "x");

            var corpo = handler.Bodies[0]!;
            Assert.Contains(@"""bankCode"":""260""", corpo);
            Assert.Contains(@"""ispbCode"":""18236120""", corpo);
            Assert.Contains(@"""typeAccount"":""PG""", corpo);
        }

        [Fact]
        public async Task PagarTedAsync_Poupanca_EnviaPP()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("READY_TO_PAY")), (HttpStatusCode.OK, Resposta("PENDING_CONFIRMATION")));
            var movimento = CriarMovimento(i => i.TipoConta = TipoContaEnum.ContaPoupanca);

            await CriarClient(handler).PagarTedAsync(movimento, CriarOrigem(), "x");

            Assert.Contains(@"""typeAccount"":""PP""", handler.Bodies[0]!);
        }

        [Fact]
        public async Task PagarTedAsync_Cnpj_EnviaCnpj()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("READY_TO_PAY")), (HttpStatusCode.OK, Resposta("PENDING_CONFIRMATION")));
            var movimento = CriarMovimento();
            movimento.Favorecido!.TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ;
            movimento.Favorecido.CPF_CNPJ = "12.345.678/0001-00";

            await CriarClient(handler).PagarTedAsync(movimento, CriarOrigem(), "x");

            Assert.Contains(@"""legalEntityIdentifier"":""CNPJ"",""documentIdentifierNumber"":""12345678000100""", handler.Bodies[0]!);
        }

        [Fact]
        public async Task PagarTedAsync_RejeitadaNaIniciacao_NaoConfirma()
        {
            var handler = Sequencia((HttpStatusCode.Created, Resposta("REJECTED", @", ""rejectReason"": ""CONTA_INVALIDA""")));
            var movimento = CriarMovimento();

            var resultado = await CriarClient(handler).PagarTedAsync(movimento, CriarOrigem(), "x");

            Assert.Equal(1, handler.Requests.Count);
            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, resultado.SituacaoBancoBr);
            Assert.Equal("CONTA_INVALIDA", resultado.DetalheRejeicaoBancoBr);
        }

        [Fact]
        public async Task PagarTedAsync_SemFinalidade_Lanca()
        {
            var client = CriarClient(Sequencia());
            var movimento = CriarMovimento(i => i.CodigoFinalidadeTED = FinalidadeTEDEnum.NaoAplicavel);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.PagarTedAsync(movimento, CriarOrigem(), "x"));
        }

        [Fact]
        public async Task PagarTedAsync_SemFavorecido_Lanca()
        {
            var client = CriarClient(Sequencia());
            var movimento = CriarMovimento();
            movimento.Favorecido = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.PagarTedAsync(movimento, CriarOrigem(), "x"));
        }

        [Fact]
        public async Task ConsultarTedAsync_Efetivada_MapeiaSituacaoEAutenticacao()
        {
            var handler = Sequencia((HttpStatusCode.OK, Resposta("PAYED", @", ""transaction"": { ""value"": ""2500.65"", ""code"": ""VXBT202403252085158bd9a"", ""date"": ""2026-10-12"" }")));
            var movimento = CriarMovimento();
            movimento.NumeroDocumentoNoBanco = TransferId;

            var resultado = await CriarClient(handler).ConsultarTedAsync(movimento, CriarOrigem());

            var get = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, get.Method);
            Assert.Equal($"{BaseUrl}/workspaces/ws-1/transfer/{TransferId}", get.RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal(new DateTime(2026, 10, 12), resultado.HorarioBancoBr);
            Assert.Equal("VXBT202403252085158bd9a", ((MovimentoItemTransferenciaTED)resultado.MovimentoItem).CodigoAutenticacao);
        }

        [Fact]
        public async Task ConsultarTedAsync_SemId_Lanca()
        {
            var client = CriarClient(Sequencia());

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConsultarTedAsync(CriarMovimento(), CriarOrigem()));
        }

        [Fact]
        public async Task SemIdWorkspace_Lanca()
        {
            var client = new PagamentoTedClient(new HttpClient(Sequencia()), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.PagarTedAsync(CriarMovimento(), CriarOrigem(), "x"));
            Assert.Contains("idWorkspace", ex.Message);
        }

        [Fact]
        public async Task CancelarAgendamentoAsync_NaoSuportado()
        {
            var client = CriarClient(Sequencia());

            await Assert.ThrowsAsync<NotSupportedException>(() => client.CancelarAgendamentoAsync(CriarMovimento(), "x"));
        }
    }
}

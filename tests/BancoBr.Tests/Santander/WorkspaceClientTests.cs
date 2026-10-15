using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Santander.Pagamentos.Workspaces;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Xunit;

namespace BancoBr.Tests.Santander
{
    /// <summary>Endpoint único de workspace: acha, habilita ou cria a workspace da conta pagadora e devolve o id.</summary>
    public class WorkspaceClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://trust-open.api.santander.com.br/management_payments_partners/v1");

        private static WorkspaceClient CriarClient(HttpMessageHandler handler) =>
            new WorkspaceClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, CriarOrigem());

        private static Correntista CriarOrigem(BancoEnum banco = BancoEnum.Santander) => new Correntista { Banco = (int)banco, NumeroAgencia = 1234, NumeroConta = 5678, DVConta = "9" };

        private static string Workspace(string id, string status = "ACTIVE", string agencia = "1234", string conta = "000000056789", string produtos = "", string extras = "") => $@"{{
            ""workspace_id"": ""{id}"", ""status"": ""{status}"", ""type"": ""PAYMENTS"", ""description"": ""Minha workspace"",
            ""mainDebitAccount"": {{ ""branch"": ""{agencia}"", ""number"": ""{conta}"" }}{(produtos == "" ? "" : ", " + produtos)}{extras}
        }}";

        private static string Lista(params string[] workspaces) =>
            $@"{{ ""_pageable"": {{ ""_totalElements"": {workspaces.Length} }}, ""_content"": [ {string.Join(",", workspaces)} ] }}";

        private const string TodosAtivos =
            @"""pixPaymentsActive"": true, ""bankSlipPaymentsActive"": true, ""bankSlipAvailableActive"": true, ""barCodePaymentsActive"": true, ""bankTransferPaymentsActive"": true";

        [Fact]
        public async Task WorkspaceExistenteComTodosOsProdutos_DevolveIdSemAlterar()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, Lista(Workspace("ws-1", produtos: TodosAtivos))));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-1", id);
            var get = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, get.Method);
            Assert.Equal("fake-client-id", get.Headers.GetValues("X-Application-Key").Single());
        }

        [Fact]
        public async Task WorkspaceComProdutoDesligado_HabilitaViaPatchPreservandoOsDemaisDados()
        {
            var ws = Workspace("ws-1", produtos: @"""pixPaymentsActive"": true, ""bankSlipPaymentsActive"": true",
                extras: @", ""webhookURL"": ""https://erp.exemplo/dda"", ""tags"": [""a""], ""addicionalDebitAccounts"": [ { ""branch"": ""0001"", ""number"": ""00000000000A"" } ]");
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, Lista(ws)), (HttpStatusCode.OK, @"{ ""id"": ""ws-1"" }"));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-1", id);
            Assert.Equal(2, handler.Requests.Count);
            var patch = handler.Requests[1];
            Assert.Equal("PATCH", patch.Method.Method);
            Assert.EndsWith("/workspaces/ws-1", patch.RequestUri!.ToString());
            var body = handler.Bodies[1]!;
            foreach (var flag in new[] { "pixPaymentsActive", "bankSlipPaymentsActive", "bankSlipAvailableActive", "barCodePaymentsActive", "bankTransferPaymentsActive" })
                Assert.Contains($@"""{flag}"":true", body);
            Assert.Contains(@"""mainDebitAccount"":{""branch"":""1234"",""number"":""000000056789""}", body);
            Assert.Contains(@"""additionalDebitAccounts"":[{""branch"":""0001"",""number"":""00000000000A""}]", body);
            Assert.DoesNotContain("addicionalDebitAccounts", body);
            Assert.Contains(@"""webhookURL"":""https://erp.exemplo/dda""", body);
            Assert.Contains(@"""description"":""Minha workspace""", body);
            Assert.DoesNotContain(@"""id""", body);
            Assert.DoesNotContain(@"""workspace_id""", body);
            Assert.DoesNotContain(@"""status""", body);
            Assert.DoesNotContain(@"""type""", body);
        }

        [Fact]
        public async Task WorkspaceInexistente_CriaComTodosOsProdutosLigados()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, @"{ ""_pageable"": { ""_totalElements"": 0 }, ""_content"": [] }"),
                (HttpStatusCode.Created, @"{ ""id"": ""ws-nova"", ""status"": ""ACTIVE"", ""type"": ""PAYMENTS"" }"));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-nova", id);
            var criacao = handler.Requests[1];
            Assert.Equal(HttpMethod.Post, criacao.Method);
            Assert.EndsWith("/workspaces", criacao.RequestUri!.ToString());
            var body = handler.Bodies[1]!;
            Assert.Contains(@"""type"":""PAYMENTS""", body);
            Assert.Contains(@"""mainDebitAccount"":{""branch"":""1234"",""number"":""000000056789""}", body);
            foreach (var flag in new[] { "pixPaymentsActive", "bankSlipPaymentsActive", "bankSlipAvailableActive", "barCodePaymentsActive", "bankTransferPaymentsActive" })
                Assert.Contains($@"""{flag}"":true", body);
        }

        [Fact]
        public async Task WorkspaceDeOutraContaOuInativa_NaoEhUsada()
        {
            var lista = Lista(
                Workspace("ws-outra", conta: "000000099999", produtos: TodosAtivos),
                Workspace("ws-inativa", status: "INACTIVE", produtos: TodosAtivos));
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, lista), (HttpStatusCode.Created, @"{ ""id"": ""ws-nova"" }"));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-nova", id);
        }

        [Fact]
        public async Task ContaAdicional_TambemLocalizaAWorkspace()
        {
            var ws = @"{ ""workspace_id"": ""ws-1"", ""status"": ""ACTIVE"", ""type"": ""PAYMENTS"", " + TodosAtivos + @",
                ""mainDebitAccount"": { ""branch"": ""0001"", ""number"": ""000000000001"" },
                ""additionalDebitAccounts"": [ { ""branch"": ""1234"", ""number"": ""56789"" } ] }";
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, Lista(ws)));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-1", id);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task VariasWorkspacesDaConta_PreferesAComMaisProdutosLigados()
        {
            var lista = Lista(
                Workspace("ws-pouco", produtos: @"""pixPaymentsActive"": true"),
                Workspace("ws-completa", produtos: TodosAtivos));
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, lista));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-completa", id);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task ListagemPaginada_PercorreTodasAsPaginas()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, @"{ ""_pageable"": { ""_totalElements"": 2 }, ""_content"": [ " + Workspace("ws-outra", agencia: "0001", conta: "1", produtos: TodosAtivos) + " ] }"),
                (HttpStatusCode.OK, Lista(Workspace("ws-1", produtos: TodosAtivos)).Replace(@"""_totalElements"": 1", @"""_totalElements"": 2")));

            var id = await CriarClient(handler).ObterOuCriarWorkspaceAsync();

            Assert.Equal("ws-1", id);
            Assert.Contains("_offset=0", handler.Requests[0].RequestUri!.ToString());
            Assert.Contains("_offset=1", handler.Requests[1].RequestUri!.ToString());
        }

        [Fact]
        public async Task SemOrigem_Lanca()
        {
            var handler = new SequencedFakeHttpMessageHandler();

            await Assert.ThrowsAsync<ArgumentNullException>(() => new WorkspaceClient(new HttpClient(handler), new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl, null!).ObterOuCriarWorkspaceAsync());
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ErroDoBanco_ViraBancoApiException()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.Forbidden, @"{ ""_errorCode"": ""403"", ""_message"": ""Forbidden"", ""_details"": ""Sem permissão"" }"));

            var ex = await Assert.ThrowsAsync<BancoApiException>(() => CriarClient(handler).ObterOuCriarWorkspaceAsync());

            Assert.Equal(403, ex.HttpStatusCode);
        }

        #region ::. BancoApi .::

        private static CertificateSource CriarCertificateSourceFake()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=fake", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificado = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            return CertificateSource.FromPfxBytes(certificado.Export(X509ContentType.Pfx, "senha"), "senha");
        }

        [Fact]
        public void Conectar_Santander_AceitaIdWorkspaceOpcional()
        {
            var cert = CriarCertificateSourceFake();

            Assert.NotNull(BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "fake-client-id", CertificadoDigital = cert, Correntista = CriarOrigem(BancoEnum.Santander) }, new FakeOAuthTokenProvider()));
            Assert.NotNull(BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "fake-client-id", CertificadoDigital = cert, Correntista = CriarOrigem(BancoEnum.Santander), IdWorkspace = "ws-1" }, new FakeOAuthTokenProvider()));
        }

        [Fact]
        public async Task ObterOuCriarWorkspaceAsync_Sicoob_NaoSuportado()
        {
            var banco = BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "fake-client-id", CertificadoDigital = CriarCertificateSourceFake(), Correntista = CriarOrigem(BancoEnum.Sicoob) }, new FakeOAuthTokenProvider());

            await Assert.ThrowsAsync<NotSupportedException>(() => banco.ObterOuCriarWorkspaceAsync());
        }

        #endregion
    }
}

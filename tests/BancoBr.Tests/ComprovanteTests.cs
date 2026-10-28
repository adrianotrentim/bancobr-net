using System.Net;
using BancoBr.API.Core.Errors;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BancoBr.Tests
{
    public class ComprovanteTests
    {
        private static Movimento CriarMovimento(string modalidade)
        {
            MovimentoItem item = modalidade switch
            {
                "boleto" => new MovimentoItemPagamentoTituloCodigoBarra(),
                "tributo" => new MovimentoItemPagamentoConvenioCodigoBarra(),
                "ted" => new MovimentoItemTransferenciaTED(),
                "qrcode" => new MovimentoItemPagamentoTituloPIXQRCode(),
                _ => new MovimentoItemTransferenciaPIX()
            };
            return new Movimento { NumeroDocumentoNoBanco = "id", MovimentoItem = item };
        }

        [Theory]
        [InlineData(33, "boleto", "bank_slip_payments")]
        [InlineData(33, "tributo", "barcode_payments")]
        [InlineData(33, "pix", "pix_payments")]
        [InlineData(33, "ted", "transfer")]
        [InlineData(33, "qrcode", "pix_payments")]
        [InlineData(748, "boleto", "boletos")]
        [InlineData(748, "tributo", "tributos/barras")]
        [InlineData(748, "pix", "pix")]
        public async Task ConsultarComprovante_BancosSuportados_RetornaTrueEPreencheMesmoCampo(int banco, string modalidade, string recurso)
        {
            var handler = new ComprovanteHandler();
            var movimento = CriarMovimento(modalidade);
            var api = CompatibilidadeApiTests.Criar(banco, handler);

            bool obtido = await api.ConsultarComprovanteAsync(movimento);

            Assert.True(obtido);
            Assert.Equal(handler.Pdf, movimento.ArquivoComprovantePDF);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, movimento.SituacaoBancoBr);
            Assert.Contains(handler.Requests, r => r.RequestUri!.AbsolutePath.EndsWith("/" + recurso + "/id"));
            Assert.Single(handler.Requests.Where(ComprovanteHandler.Download));
            var json = JObject.Parse(JsonConvert.SerializeObject(movimento));
            Assert.Equal(Convert.ToBase64String(movimento.ArquivoComprovantePDF), (string?)json[nameof(Movimento.ArquivoComprovantePDF)]);
            Assert.Null(json["LinkComprovanteBancoBr"]);
            Assert.Null(json["ValidadeLinkComprovanteBancoBr"]);
        }

        [Theory]
        [InlineData(33)]
        [InlineData(748)]
        public async Task ConsultarComprovante_PagamentoPendente_RetornaFalseSemBaixarArquivo(int banco)
        {
            var handler = new ComprovanteHandler { PagamentoPendente = true };
            var movimento = CriarMovimento("pix");
            movimento.ArquivoComprovantePDF = new byte[] { 1, 2 };

            var obtido = await CompatibilidadeApiTests.Criar(banco, handler).ConsultarComprovanteAsync(movimento);

            Assert.False(obtido);
            Assert.Null(movimento.ArquivoComprovantePDF);
            Assert.Equal(BancoBrSituacaoEnum.Agendado, movimento.SituacaoBancoBr);
            Assert.Single(handler.Requests);
        }

        [Fact]
        public async Task ConsultarComprovante_SantanderEmGeracao_ReaproveitaSolicitacaoNaProximaConsulta()
        {
            var handler = new ComprovanteHandler { EmGeracao = true };
            var movimento = CriarMovimento("pix");
            var api = CompatibilidadeApiTests.Criar(33, handler);

            Assert.False(await api.ConsultarComprovanteAsync(movimento));
            Assert.Null(movimento.ArquivoComprovantePDF);
            Assert.Equal(5, handler.Requests.Count(r => r.RequestUri!.AbsolutePath.EndsWith("/pedido")));
            Assert.DoesNotContain(handler.Requests, ComprovanteHandler.Download);

            handler.EmGeracao = false;
            Assert.True(await api.ConsultarComprovanteAsync(movimento));
            Assert.Equal(handler.Pdf, movimento.ArquivoComprovantePDF);
            Assert.All(handler.Requests, r => Assert.Equal(HttpMethod.Get, r.Method));
        }

        [Fact]
        public async Task ConsultarComprovante_SantanderSemSolicitacao_CriaAntesDeBaixar()
        {
            var handler = new ComprovanteHandler { SemSolicitacao = true };

            var movimento = CriarMovimento("pix");
            var obtido = await CompatibilidadeApiTests.Criar(33, handler).ConsultarComprovanteAsync(movimento);

            Assert.True(obtido);
            Assert.Equal(handler.Pdf, movimento.ArquivoComprovantePDF);
            var post = Assert.Single(handler.Requests.Where(r => r.Method == HttpMethod.Post));
            Assert.EndsWith("/file_requests", post.RequestUri!.AbsolutePath);
        }

        [Theory]
        [InlineData(33)]
        [InlineData(748)]
        public async Task ConsultarComprovante_DownloadRejeitado_NormalizaErroSemPreencherArquivo(int banco)
        {
            var handler = new ComprovanteHandler { DownloadStatus = HttpStatusCode.BadGateway };
            var movimento = CriarMovimento("pix");
            movimento.ArquivoComprovantePDF = new byte[] { 1, 2 };

            var erro = await Assert.ThrowsAsync<BancoApiException>(() => CompatibilidadeApiTests.Criar(banco, handler).ConsultarComprovanteAsync(movimento));

            Assert.Equal(502, erro.HttpStatusCode);
            Assert.Contains("Falha no arquivo", erro.Message);
            Assert.Null(movimento.ArquivoComprovantePDF);
        }

        [Theory]
        [InlineData(33)]
        [InlineData(748)]
        public async Task ConsultarComprovante_CancelamentoDuranteDownload_PropagaSemPreencherArquivo(int banco)
        {
            using var cancelamento = new CancellationTokenSource();
            var handler = new ComprovanteHandler { BloquearDownload = true };
            var movimento = CriarMovimento("pix");
            var consulta = CompatibilidadeApiTests.Criar(banco, handler).ConsultarComprovanteAsync(movimento, cancelamento.Token);
            await handler.DownloadIniciado.Task.WaitAsync(TimeSpan.FromSeconds(5));

            cancelamento.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consulta);
            Assert.Null(movimento.ArquivoComprovantePDF);
        }

        private sealed class ComprovanteHandler : HttpMessageHandler
        {
            internal readonly byte[] Pdf = { 37, 80, 68, 70, 45, 255, 0, 128 };
            internal readonly List<HttpRequestMessage> Requests = new();
            internal readonly TaskCompletionSource<bool> DownloadIniciado = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal bool PagamentoPendente;
            internal bool EmGeracao;
            internal bool SemSolicitacao;
            internal bool BloquearDownload;
            internal HttpStatusCode DownloadStatus = HttpStatusCode.OK;

            internal static bool Download(HttpRequestMessage request)
                => request.RequestUri!.Host == "files.example.test" || request.RequestUri.AbsolutePath.EndsWith("/comprovantes");

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Requests.Add(request);
                if (Download(request))
                {
                    Assert.Equal("application/pdf", request.Headers.Accept.Single().MediaType);
                    if (request.RequestUri!.Host == "files.example.test")
                    {
                        Assert.Null(request.Headers.Authorization);
                        Assert.False(request.Headers.Contains("X-Application-Key"));
                    }
                    DownloadIniciado.TrySetResult(true);
                    if (BloquearDownload) await Task.Delay(Timeout.Infinite, ct);
                    if (DownloadStatus != HttpStatusCode.OK)
                        return new HttpResponseMessage(DownloadStatus) { Content = new StringContent("{\"detail\":\"Falha no arquivo\",\"_message\":\"Falha no arquivo\"}") };
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Pdf) };
                }

                var path = request.RequestUri!.AbsolutePath;
                if (path.EndsWith("/file_requests"))
                {
                    if (request.Method == HttpMethod.Post) return Json("{\"request\":{\"requestId\":\"pedido\"}}");
                    return Json(SemSolicitacao ? "{\"paymentReceiptsFileRequests\":[]}"
                        : "{\"paymentReceiptsFileRequests\":[{\"request\":{\"requestId\":\"pedido\"},\"file\":{\"statusInfo\":{\"statusCode\":\"REQUESTED\"}}}]}");
                }
                if (path.EndsWith("/pedido"))
                    return Json(EmGeracao ? "{\"file\":{\"statusInfo\":{\"statusCode\":\"REQUESTED\"}}}"
                        : "{\"file\":{\"statusInfo\":{\"statusCode\":\"AVAILABLE\"},\"fileRepository\":{\"location\":\"https://files.example.test/arquivo.pdf?assinatura=ficticia\"}}}");
                if (path.EndsWith("/id"))
                {
                    if (request.RequestUri.Host == "mtls-api-parceiro.sicredi.com.br")
                        return Json(PagamentoPendente ? "{\"status\":\"AGUARDANDO_APROVACAO\"}" : "{\"status\":\"SUCESSO\"}");
                    return Json(PagamentoPendente ? "{\"status\":\"PENDING_CONFIRMATION\",\"transaction\":{\"code\":\"transacao\"}}"
                        : "{\"status\":\"PAYED\",\"transaction\":{\"code\":\"transacao\"}}");
                }
                throw new InvalidOperationException("Requisição inesperada no teste: " + path);
            }

            private static HttpResponseMessage Json(string body)
                => new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
        }
    }
}

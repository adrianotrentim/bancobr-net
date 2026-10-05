using System.Net;
using BancoBr.API.Core.Errors;
using BancoBr.API.Sicoob.Pagamentos.Convenios;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Xunit;

namespace BancoBr.Tests.Sicoob
{
    public class PagamentoConvenioClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/convenios-pagamentos/v2");

        private const string CodigoBarras = "00000000000000000000000000000000000000000000";

        private static PagamentoConvenioClient CriarClient(FakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoConvenioClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        private static PagamentoConvenioClient CriarClient(SequencedFakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoConvenioClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        /// <summary>A "instituicao" da arrecadação vem do NumeroAgencia da conta pagadora.</summary>
        private static Correntista CriarOrigem(int instituicao = 1234) => new Correntista
        {
            NumeroAgencia = instituicao,
            NumeroConta = 1234569,
            Nome = "Empresa Teste",
            CPF_CNPJ = "12345678000199",
            TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ,
        };

        private static Movimento CriarMovimento(DateTime? dataPagamento = null, long? transacao = null) => new Movimento
        {
            DataPagamento = dataPagamento ?? new DateTime(2026, 6, 29),
            MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra
            {
                CodigoBarra = CodigoBarras,
                Transacao = transacao,
            },
        };

        private static MovimentoItemPagamentoConvenioCodigoBarra Item(Movimento movimento) =>
            (MovimentoItemPagamentoConvenioCodigoBarra)movimento.MovimentoItem;

        [Fact]
        public async Task ConsultarCodigoBarrasAsync_200_RetornaDadosDoConvenio()
        {
            var json = @"
            {
              ""resultado"": {
                ""convenio"": ""1"",
                ""siglaConvenio"": ""SiG"",
                ""valorDocumento"": 1171.23,
                ""valorDesconto"": 11.23,
                ""valorMulta"": 17.23,
                ""valorJuros"": 1.71,
                ""valorOutrosEncargos"": 0,
                ""valorTotal"": 1171.23,
                ""codigoConvenioFebraban"": ""0025"",
                ""nsu"": 183390172928,
                ""transacao"": 123456789
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.ConsultarCodigoBarrasAsync(movimento, CriarOrigem());

            Assert.Same(movimento, resultado);
            Assert.Equal("SiG", Item(resultado).SiglaConvenio);
            Assert.Equal(183390172928, Item(resultado).Nsu);
            Assert.Equal(1171.23m, resultado.ValorPagamento);
            // A transação devolvida pela consulta é o que o pagamento reenvia.
            Assert.Equal(123456789, Item(resultado).Transacao);
            Assert.Equal("fake-client-id", handler.LastRequest!.Headers.GetValues("client_id").Single());
        }

        [Fact]
        public async Task PagarConvenioAsync_200_RetornaArrecadacaoEfetivada()
        {
            var json = @"
            {
              ""resultado"": {
                ""comprovante"": ""PCFbQ0RBVEFb"",
                ""arrecadacao"": {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""valorDesconto"": 11.23,
                  ""valorJuros"": 1.71,
                  ""valorMulta"": 17.23,
                  ""autenticacao"": ""71205202-2DBB-46C7-BA31-0DFB8DC64EBE"",
                  ""recebimentoViaCaixa"": false
                }
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var movimento = CriarMovimento(transacao: 123456789);
            movimento.ValorPagamento = 1171.23m;
            Item(movimento).ValorDocumento = 1171.23m;
            Item(movimento).ValorDesconto = 11.23m;
            Item(movimento).ValorJuros = 1.71m;
            Item(movimento).ValorMulta = 17.23m;

            var resultado = await client.PagarConvenioAsync(movimento, CriarOrigem());

            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal("PCFbQ0RBVEFb", Item(resultado).ComprovanteBase64);
            Assert.Equal("71205202-2DBB-46C7-BA31-0DFB8DC64EBE", Item(resultado).Autenticacao);
            Assert.Equal("183390172928", resultado.NumeroDocumentoNoBanco);
            Assert.Contains("\"instituicao\":1234", handler.LastRequestBody);
            Assert.Contains("\"transacao\":123456789", handler.LastRequestBody);
            Assert.Contains("2026-06-29", handler.LastRequestBody);
        }

        [Fact]
        public async Task PagarConvenioAsync_202_RetornaPendenteAssinatura()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Accepted);
            var client = CriarClient(handler);
            var movimento = CriarMovimento(transacao: 123456789);

            var resultado = await client.PagarConvenioAsync(movimento, CriarOrigem());

            Assert.Equal(BancoBrSituacaoEnum.Agendado, resultado.SituacaoBancoBr);
            Assert.Contains("assinatura", resultado.DetalheRejeicaoBancoBr);
            Assert.Null(Item(resultado).ComprovanteBase64);
        }

        [Fact]
        public async Task PagarConvenioAsync_400_LancaBancoApiException()
        {
            var json = @"
            {
              ""mensagens"": [ { ""mensagem"": ""Convênio não habilitado."", ""codigo"": ""20011"" } ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, json);
            var client = CriarClient(handler);

            var ex = await Assert.ThrowsAsync<BancoApiException>(() =>
                client.PagarConvenioAsync(CriarMovimento(transacao: 123456789), CriarOrigem()));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal("20011", ex.Mensagens.Single().Codigo);
        }

        [Fact]
        public async Task PagarConvenioAsync_400ComCodigo10272_RecuperaPagamentoJaEfetivado()
        {
            const string erroJson = @"
                {
                  ""mensagens"": [ { ""mensagem"": ""O idempotency já foi utilizado com sucesso em outra execução."", ""codigo"": ""10272"" } ]
                }";
            const string pagamentosJson = @"
                {
                  ""resultado"": [
                    {
                      ""valorPago"": 1171.23,
                      ""nsu"": 183390172928,
                      ""dataPagamento"": ""2026-06-29"",
                      ""valorDocumento"": 1171.23,
                      ""autenticacao"": ""71205202-2DBB-46C7-BA31-0DFB8DC64EBE"",
                      ""transacao"": 123456789
                    }
                  ]
                }";

            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.BadRequest, erroJson),
                (HttpStatusCode.OK, pagamentosJson));
            var client = CriarClient(handler);
            var movimento = CriarMovimento(transacao: 123456789);

            var resultado = await client.PagarConvenioAsync(movimento, CriarOrigem());

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            // A consulta de pagamentos não devolve o PDF do comprovante — só os dados da arrecadação.
            Assert.Null(Item(resultado).ComprovanteBase64);
            Assert.Equal("71205202-2DBB-46C7-BA31-0DFB8DC64EBE", Item(resultado).Autenticacao);
            Assert.Equal("183390172928", resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task PagarConvenioAsync_400ComCodigo10272SemPagamentoCorrespondente_LancaBancoApiException()
        {
            const string erroJson = @"
                {
                  ""mensagens"": [ { ""mensagem"": ""O idempotency já foi utilizado com sucesso em outra execução."", ""codigo"": ""10272"" } ]
                }";
            const string pagamentosJson = @"{ ""resultado"": [] }";

            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.BadRequest, erroJson),
                (HttpStatusCode.OK, pagamentosJson));
            var client = CriarClient(handler);

            var ex = await Assert.ThrowsAsync<BancoApiException>(() =>
                client.PagarConvenioAsync(CriarMovimento(transacao: 123456789), CriarOrigem()));

            Assert.Equal("10272", ex.Mensagens.Single().Codigo);
        }

        [Fact]
        public async Task ConsultarPagamentosAsync_200_MapeiaSituacaoEListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""situacao"": { ""codigo"": 0, ""descricao"": ""Recebido"" },
                  ""convenio"": ""string"",
                  ""siglaConvenio"": ""string"",
                  ""transacao"": 1234569789
                }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarPagamentosAsync(CodigoBarras, 1234, new DateTime(2026, 6, 29));

            Assert.Single(resultado);
            Assert.Equal("Recebido", resultado[0].DetalheRejeicaoBancoBr);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado[0].SituacaoBancoBr);
        }

        [Fact]
        public async Task ConsultarPagamentosAsync_SituacaoNaoReconhecida_RetornaNaoIntegrado()
        {
            var json = @"
            {
              ""resultado"": [
                {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""situacao"": { ""codigo"": 99, ""descricao"": ""Outro"" },
                  ""convenio"": ""string"",
                  ""siglaConvenio"": ""string"",
                  ""transacao"": 1234569789
                }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarPagamentosAsync(CodigoBarras, 1234, new DateTime(2026, 6, 29));

            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, resultado[0].SituacaoBancoBr);
        }

        [Fact]
        public async Task ConsultarPagamentosAsync_SituacaoRejeitado_RetornaRejeitado()
        {
            var json = @"
            {
              ""resultado"": [
                {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""situacao"": { ""codigo"": 2, ""descricao"": ""Rejeitado"" },
                  ""convenio"": ""string"",
                  ""siglaConvenio"": ""string"",
                  ""transacao"": 1234569789
                }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarPagamentosAsync(CodigoBarras, 1234, new DateTime(2026, 6, 29));

            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, resultado[0].SituacaoBancoBr);
        }

        [Fact]
        public async Task ConsultarComprovantePorNsuAsync_200_RetornaComprovante()
        {
            var json = @"
            {
              ""resultado"": {
                ""comprovante"": ""PCFbQ0RBVEFb"",
                ""pagamento"": {
                  ""valorPago"": 1171.23,
                  ""nsu"": 183390172928,
                  ""dataPagamento"": ""2026-06-29"",
                  ""valorDocumento"": 1171.23,
                  ""autenticacao"": ""71205202-2DBB-46C7-BA31-0DFB8DC64EBE""
                }
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            Item(movimento).Nsu = 183390172928;

            var resultado = await client.ConsultarComprovantePorNsuAsync(movimento, CriarOrigem());

            Assert.Equal("PCFbQ0RBVEFb", Item(resultado).ComprovanteBase64);
            Assert.Equal("71205202-2DBB-46C7-BA31-0DFB8DC64EBE", Item(resultado).Autenticacao);
            Assert.Contains("183390172928", handler.LastRequest!.RequestUri!.ToString());
        }

        [Fact]
        public async Task ConsultarConciliacoesAsync_200_RetornaListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                { ""situacao"": ""Aceito"", ""convenio"": ""1"", ""siglaConvenio"": ""SiG"", ""valorTotal"": 1171.23, ""quantidade"": 3 }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarConciliacoesAsync(1234, new DateTime(2026, 6, 29));

            Assert.Single(resultado);
            Assert.Equal("Aceito", resultado[0].Situacao);
            Assert.Equal(3, resultado[0].Quantidade);
        }

        [Fact]
        public async Task ConsultarConveniosHabilitadosAsync_200_RetornaListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                { ""identificador"": ""0183390172928"", ""sigla"": ""Sigla - 001"", ""codigoFebraban"": ""0006"", ""segmento"": 4 }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarConveniosHabilitadosAsync(123456789, 1234);

            Assert.Single(resultado);
            Assert.Equal("Sigla - 001", resultado[0].Sigla);
        }

        [Fact]
        public async Task ConsultarCodigoBarrasAsync_401_RenovaTokenERepeteChamadaUmaVez()
        {
            const string json = @"
            {
              ""resultado"": {
                ""convenio"": ""1"",
                ""siglaConvenio"": ""SiG"",
                ""valorDocumento"": 1171.23,
                ""valorTotal"": 1171.23
              }
            }";
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.Unauthorized, null),
                (HttpStatusCode.OK, json));
            var client = CriarClient(handler);

            var resultado = await client.ConsultarCodigoBarrasAsync(CriarMovimento(), CriarOrigem());

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("SiG", Item(resultado).SiglaConvenio);
        }
    }
}

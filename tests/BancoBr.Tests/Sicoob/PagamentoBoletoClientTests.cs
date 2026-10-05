using System.Net;
using BancoBr.API.Core.Models;
using BancoBr.API.Core.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Xunit;

namespace BancoBr.Tests.Sicoob
{
    public class PagamentoBoletoClientTests
    {
        private static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pagamentos/v3");

        private const string CodigoBarras = "00000000000000000000000000000000000000000000";

        private static PagamentoBoletoClient CriarClient(FakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoBoletoClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        private static PagamentoBoletoClient CriarClient(SequencedFakeHttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            return new PagamentoBoletoClient(httpClient, new FakeOAuthTokenProvider(), "fake-client-id", BaseUrl);
        }

        /// <summary>Conta pagadora usada nos testes (equivale ao numeroConta/numeroAgencia dos antigos parâmetros soltos).</summary>
        private static Correntista CriarOrigem(int numeroAgencia = 1234, int numeroConta = 1234569) => new Correntista
        {
            NumeroAgencia = numeroAgencia,
            NumeroConta = numeroConta,
            Nome = "Rosa Maria da Silva",
            CPF_CNPJ = "123.456.789-00",
            TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ,
        };

        private static Movimento CriarMovimento(string? codigoBarras = CodigoBarras) => new Movimento
        {
            MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = codigoBarras },
        };

        private static MovimentoItemPagamentoTituloCodigoBarra Item(Movimento movimento) =>
            (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem;

        [Fact]
        public async Task ConsultarBoletoAsync_200_RetornaIdentificadorConsulta()
        {
            var json = @"
            {
              ""resultado"": {
                ""numeroInstituicaoEmissora"": 756,
                ""nomeInstituicaoEmissora"": ""Banco Cooperativo do Brasil"",
                ""codigoBarras"": ""00000000000000000000000000000000000000000000"",
                ""numeroLinhaDigitavel"": ""string"",
                ""dataVencimentoBoleto"": ""2021-04-20"",
                ""valorBoleto"": 152.3,
                ""valorAbatimentoDesconto"": 0,
                ""valorMultaMora"": 0,
                ""valorPagamento"": 152.3,
                ""dataPagamento"": ""2021-04-24"",
                ""permiteAlterarValor"": true,
                ""consultaEmContingencia"": false,
                ""identificadorConsulta"": ""hash-123"",
                ""descricaoInstrucaoValorMinMax"": ""string"",
                ""bloquearPagamento"": false
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.ConsultarBoletoAsync(movimento, CriarOrigem());

            Assert.Same(movimento, resultado);
            Assert.Equal("hash-123", Item(resultado).IdentificadorConsulta);
            Assert.Equal(756, Item(resultado).BancoCodigoBarra);
            Assert.Equal(152.3m, resultado.ValorPagamento);
            Assert.Equal("fake-client-id", handler.LastRequest!.Headers.GetValues("client_id").Single());
        }

        [Fact]
        public async Task ConsultarBoletoAsync_204_SinalizaBoletoNaoEncontrado()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.ConsultarBoletoAsync(movimento, CriarOrigem());

            Assert.Equal(BancoBrSituacaoEnum.Cancelado, resultado.SituacaoBancoBr);
            Assert.Equal("Boleto não encontrado.", resultado.DetalheRejeicaoBancoBr);
            Assert.Null(Item(resultado).IdentificadorConsulta);
        }

        [Fact]
        public async Task PagarBoletoAsync_200_RetornaComprovanteEfetivado()
        {
            var json = @"
            {
              ""resultado"": {
                ""numeroAgencia"": ""0001-9"",
                ""numeroConta"": 1234569,
                ""numeroInstituicaoEmissora"": 756,
                ""dataVencimento"": ""2018-09-20"",
                ""valorBoleto"": 100.36,
                ""valorAbatimentoDesconto"": 0,
                ""valorMultaMora"": 60.36,
                ""valorPagamento"": 255.63,
                ""dataPagamento"": ""2019-10-20"",
                ""situacaoPagamento"": ""Efetivado"",
                ""idPagamento"": 1983450,
                ""numeroAutenticacaoPagamento"": ""89C3E9FD-1A37-40BE-A85B-69AF118D336A""
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var movimento = CriarMovimento();
            movimento.ValorPagamento = 255.63m;
            movimento.DataPagamento = new DateTime(2019, 10, 20);
            Item(movimento).IdentificadorConsulta = "hash-123";
            Item(movimento).ValorCodigoBarra = 100.36m;
            Item(movimento).AceitaValorDivergente = true;

            var resultado = await client.PagarBoletoAsync(movimento, CriarOrigem(), IdempotencyKey.New(1234, 1234569, Guid.NewGuid()));

            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal("1983450", resultado.NumeroDocumentoNoBanco);
            Assert.Equal("89C3E9FD-1A37-40BE-A85B-69AF118D336A", Item(resultado).NumeroAutenticacaoPagamento);
            Assert.Contains("\"identificadorConsulta\":\"hash-123\"", handler.LastRequestBody);
            Assert.Contains("2019-10-20", handler.LastRequestBody);
            // A conta pagadora (Correntista) substitui os antigos parâmetros soltos de portador/conta.
            Assert.Contains("\"numeroCpfCnpjPortador\":\"12345678900\"", handler.LastRequestBody);
            Assert.Contains("\"issuer\":1234", handler.LastRequestBody);
            Assert.Contains("\"number\":1234569", handler.LastRequestBody);
        }

        [Fact]
        public async Task PagarBoletoAsync_202_RetornaPendenteAssinatura()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.Accepted);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.PagarBoletoAsync(movimento, CriarOrigem(), IdempotencyKey.New(1234, 1234569, Guid.NewGuid()));

            Assert.Equal(BancoBrSituacaoEnum.Agendado, resultado.SituacaoBancoBr);
            Assert.Contains("assinatura", resultado.DetalheRejeicaoBancoBr);
            Assert.Null(resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task PagarBoletoAsync_204_RetornaEfetivadoSemComprovante()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.PagarBoletoAsync(movimento, CriarOrigem(), IdempotencyKey.New(1234, 1234569, Guid.NewGuid()));

            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Null(resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task PagarBoletoAsync_400_LancaBancoApiException()
        {
            var json = @"
            {
              ""mensagens"": [ { ""mensagem"": ""Saldo insuficiente para o lançamento."", ""codigo"": ""10013"" } ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest, json);
            var client = CriarClient(handler);

            var ex = await Assert.ThrowsAsync<BancoApiException>(() =>
                client.PagarBoletoAsync(CriarMovimento(), CriarOrigem(), IdempotencyKey.New(1234, 1234569, Guid.NewGuid())));

            Assert.Equal(400, ex.HttpStatusCode);
            Assert.Equal("10013", ex.Mensagens.Single().Codigo);
        }

        [Fact]
        public async Task PagarBoletoAsync_400ComCodigo10272_RecuperaComprovantePorIdempotency()
        {
            const string erroJson = @"
                {
                  ""mensagens"": [ { ""mensagem"": ""O idempotency já foi utilizado com sucesso em outra execução."", ""codigo"": ""10272"" } ]
                }";

            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.BadRequest, erroJson),
                (HttpStatusCode.OK, ComprovanteJson));
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            var idempotencyKey = IdempotencyKey.New(1234, 1234569, Guid.NewGuid());

            var resultado = await client.PagarBoletoAsync(movimento, CriarOrigem(), idempotencyKey);

            Assert.Equal(2, handler.Requests.Count);
            Assert.Contains(idempotencyKey, handler.Requests[1].RequestUri!.ToString());
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal("1983450", resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task ConsultarComprovantePorIdAsync_200_RetornaBancoBrSituacaoEfetivado()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, ComprovanteJson);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            movimento.NumeroDocumentoNoBanco = "1983450";

            var resultado = await client.ConsultarComprovantePorIdAsync(movimento, CriarOrigem());

            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal("1983450", resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task ConsultarComprovantePorIdAsync_SituacaoNaoReconhecida_RetornaNaoIntegrado()
        {
            const string json = @"
            {
              ""resultado"": {
                ""idPagamento"": 1983450,
                ""situacaoPagamento"": ""XYZ""
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            movimento.NumeroDocumentoNoBanco = "1983450";

            var resultado = await client.ConsultarComprovantePorIdAsync(movimento, CriarOrigem());

            Assert.Equal(BancoBrSituacaoEnum.NaoIntegrado, resultado.SituacaoBancoBr);
        }

        [Fact]
        public async Task ConsultarComprovantePorIdAsync_SituacaoRejeitado_RetornaRejeitado()
        {
            const string json = @"
            {
              ""resultado"": {
                ""idPagamento"": 1983450,
                ""situacaoPagamento"": ""Rejeitado""
              }
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            movimento.NumeroDocumentoNoBanco = "1983450";

            var resultado = await client.ConsultarComprovantePorIdAsync(movimento, CriarOrigem());

            Assert.Equal(BancoBrSituacaoEnum.Rejeitado, resultado.SituacaoBancoBr);
        }

        [Fact]
        public async Task ConsultarComprovantePorIdempotencyAsync_200_MontaMovimentoAPartirDoComprovante()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, ComprovanteJson);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarComprovantePorIdempotencyAsync("1234-1234569-" + Guid.NewGuid().ToString("D"));

            Assert.NotNull(resultado);
            Assert.Equal("1983450", resultado!.NumeroDocumentoNoBanco);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.IsType<MovimentoItemPagamentoTituloCodigoBarra>(resultado.MovimentoItem);
        }

        [Fact]
        public async Task CancelarAgendamentoAsync_204_NaoLancaExcecao()
        {
            var handler = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
            var client = CriarClient(handler);
            var movimento = CriarMovimento();
            movimento.NumeroDocumentoNoBanco = "1983450";

            var resultado = await client.CancelarAgendamentoAsync(movimento, CriarOrigem());

            Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, resultado.SituacaoBancoBr);
        }

        [Fact]
        public async Task ConsultarBoletosDdaAsync_200_MapeiaSituacaoEListaDeItens()
        {
            var json = @"
            {
              ""resultado"": [
                {
                  ""valorBoleto"": 100.0,
                  ""dataVencimentoBoleto"": ""2026-06-27"",
                  ""codigoTipoSituacaoBoleto"": 1,
                  ""dataEmissao"": ""2026-06-01"",
                  ""numeroCodigoBarras"": ""string""
                }
              ]
            }";
            var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
            var client = CriarClient(handler);

            var resultado = await client.ConsultarBoletosDdaAsync(
                1234569,
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 30),
                BancoBr.API.Base.Models.SituacaoBoletoEnum.EmAberto,
                BancoBr.API.Base.Models.TipoDataConsultaEnum.Vencimento);

            Assert.Single(resultado);
            var item = Assert.IsType<MovimentoItemDDA>(resultado[0].MovimentoItem);
            Assert.Equal(1, item.CodigoTipoSituacaoBoleto);
        }

        [Fact]
        public void IdempotencyKey_New_CombinaCooperativaContaEIdLancamento()
        {
            var idLancamento = Guid.Parse("89c3e9fd-1a37-40be-a85b-69af118d336a");

            Assert.Equal("1234-1234569-" + idLancamento.ToString("D"), IdempotencyKey.New(1234, 1234569, idLancamento));
        }

        [Fact]
        public void IdempotencyKey_New_MesmoLancamentoGeraMesmaKey()
        {
            var idLancamento = Guid.NewGuid();

            var key1 = IdempotencyKey.New(1234, 1234569, idLancamento);
            var key2 = IdempotencyKey.New(1234, 1234569, idLancamento);

            Assert.Equal(key1, key2);
        }

        [Fact]
        public void IdempotencyKey_New_IdLancamentoVazio_LancaArgumentException()
        {
            Assert.Throws<ArgumentException>(() => IdempotencyKey.New(1234, 1234569, Guid.Empty));
        }

        [Theory]
        [InlineData(99999, 1234569)]
        [InlineData(1234, 999999999999999)]
        public void IdempotencyKey_New_CooperativaOuContaExcedeDigitos_LancaArgumentException(int numeroCooperativa, long numeroContaCorrente)
        {
            Assert.Throws<ArgumentException>(() => IdempotencyKey.New(numeroCooperativa, numeroContaCorrente, Guid.NewGuid()));
        }

        private const string ConsultaJsonNaoBloqueado = @"
            {
              ""resultado"": {
                ""numeroInstituicaoEmissora"": 756,
                ""dataVencimentoBoleto"": ""2021-04-20"",
                ""valorBoleto"": 152.3,
                ""valorAbatimentoDesconto"": 0,
                ""valorMultaMora"": 0,
                ""valorPagamento"": 152.3,
                ""dataPagamento"": ""2021-04-24"",
                ""identificadorConsulta"": ""hash-123"",
                ""bloquearPagamento"": false
              }
            }";

        private const string ConsultaJsonBloqueado = @"
            {
              ""resultado"": {
                ""numeroInstituicaoEmissora"": 756,
                ""dataVencimentoBoleto"": ""2021-04-20"",
                ""valorBoleto"": 152.3,
                ""dataPagamento"": ""2021-04-24"",
                ""identificadorConsulta"": ""hash-123"",
                ""bloquearPagamento"": true,
                ""mensagemBloqueioPagamento"": ""Pagamento bloqueado""
              }
            }";

        private const string ComprovanteJson = @"
            {
              ""resultado"": {
                ""idPagamento"": 1983450,
                ""situacaoPagamento"": ""Efetivado""
              }
            }";

        [Fact]
        public async Task PagarBoletoComConsultaAsync_ConsultaOk_ConsultaEPagaEm2Chamadas()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, ConsultaJsonNaoBloqueado),
                (HttpStatusCode.OK, ComprovanteJson));
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.PagarBoletoComConsultaAsync(movimento, CriarOrigem(4342, 1234569), Guid.NewGuid());

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultado.SituacaoBancoBr);
            Assert.Equal("1983450", resultado.NumeroDocumentoNoBanco);
            // O IdentificadorConsulta da consulta é reaproveitado no pagamento.
            Assert.Equal("hash-123", Item(resultado).IdentificadorConsulta);
        }

        [Fact]
        public async Task PagarBoletoComConsultaAsync_BoletoNaoEncontrado_NaoChamaPagamento()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.NoContent, null));
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.PagarBoletoComConsultaAsync(movimento, CriarOrigem(4342, 1234569), Guid.NewGuid());

            Assert.Single(handler.Requests);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, resultado.SituacaoBancoBr);
            Assert.Equal("Boleto não encontrado.", resultado.DetalheRejeicaoBancoBr);
            Assert.Null(resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task PagarBoletoComConsultaAsync_PagamentoBloqueado_NaoChamaPagamento()
        {
            var handler = new SequencedFakeHttpMessageHandler((HttpStatusCode.OK, ConsultaJsonBloqueado));
            var client = CriarClient(handler);
            var movimento = CriarMovimento();

            var resultado = await client.PagarBoletoComConsultaAsync(movimento, CriarOrigem(4342, 1234569), Guid.NewGuid());

            Assert.Single(handler.Requests);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, resultado.SituacaoBancoBr);
            Assert.Equal("Pagamento bloqueado", resultado.DetalheRejeicaoBancoBr);
            Assert.Null(resultado.NumeroDocumentoNoBanco);
        }

        [Fact]
        public async Task ConsultarBoletoAsync_401_RenovaTokenERepeteChamadaUmaVez()
        {
            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.Unauthorized, null),
                (HttpStatusCode.OK, ConsultaJsonNaoBloqueado));
            var client = CriarClient(handler);

            var resultado = await client.ConsultarBoletoAsync(CriarMovimento(), CriarOrigem());

            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal("hash-123", Item(resultado).IdentificadorConsulta);
        }

        [Fact]
        public async Task PagarLoteBoletosAsync_ItemComErro_NaoInterrompeOsDemais()
        {
            const string erroJson = @"
                {
                  ""mensagens"": [ { ""mensagem"": ""Saldo insuficiente para o lançamento."", ""codigo"": ""10013"" } ]
                }";

            var handler = new SequencedFakeHttpMessageHandler(
                (HttpStatusCode.OK, ConsultaJsonNaoBloqueado),  // item 1: consulta
                (HttpStatusCode.OK, ComprovanteJson),           // item 1: pagamento
                (HttpStatusCode.OK, ConsultaJsonBloqueado),     // item 2: consulta (bloqueado, sem 2ª chamada)
                (HttpStatusCode.BadRequest, erroJson));         // item 3: consulta falha

            var client = CriarClient(handler);
            var itens = new[]
            {
                (Movimento: CriarMovimento("boleto-1"), IdLancamento: Guid.NewGuid()),
                (Movimento: CriarMovimento("boleto-2"), IdLancamento: Guid.NewGuid()),
                (Movimento: CriarMovimento("boleto-3"), IdLancamento: Guid.NewGuid()),
            };

            var resultados = await client.PagarLoteBoletosAsync(itens, CriarOrigem(4342, 1234569));

            Assert.Equal(3, resultados.Count);

            Assert.True(resultados[0].Sucesso);
            Assert.Equal(BancoBrSituacaoEnum.Efetivado, resultados[0].Movimento.SituacaoBancoBr);
            Assert.Equal("1983450", resultados[0].Movimento.NumeroDocumentoNoBanco);

            Assert.True(resultados[1].Sucesso);
            Assert.Equal(BancoBrSituacaoEnum.Cancelado, resultados[1].Movimento.SituacaoBancoBr);
            Assert.Equal("Pagamento bloqueado", resultados[1].Movimento.DetalheRejeicaoBancoBr);

            Assert.False(resultados[2].Sucesso);
            Assert.IsType<BancoApiException>(resultados[2].Erro);
            Assert.Equal("10013", ((BancoApiException)resultados[2].Erro).Mensagens.Single().Codigo);
            // O IdLancamento continua acessível para o ERP correlacionar a falha.
            Assert.Equal(itens[2].IdLancamento, resultados[2].IdLancamento);
        }
    }
}

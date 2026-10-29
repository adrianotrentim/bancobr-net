using System.Net;
using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.Tests.Sicoob;
using Newtonsoft.Json;
using Xunit;

namespace BancoBr.Tests
{
    public class CompatibilidadeApiTests
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
        public async Task EnviarAgendamentoAsync_ModelosExistentes_PreservaInstanciaEEstados(int banco, BancoBrSituacaoEnum esperado)
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

    }
}

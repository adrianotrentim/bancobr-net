using BancoBr.CNAB;
using BancoBr.CNAB.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Xunit;

namespace BancoBr.Tests
{
    public class CriaBancoTest
    {
        /// <summary>
        /// Todas as informações podem ser adicionadas com formatações e acentos, pois a biblioteca cuidará de retira-los
        /// Todas as informações podem ser adicionadas em minúsculo, pois a biblioteca formatará em maiúsculo
        ///
        /// No caso de CEP, é um inteiro. A biblioteca cuidará para adicionar zeros a esquerda se necessário
        /// </summary>
        [Fact]
        public void CriaBancoBradesco()
        {
            var numeroArquivo = 1;
            var tipoServico = TipoServicoEnum.PagamentoSalarios;

            var empresa = new Pessoa
            {
                TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ,
                CPF_CNPJ = "12.345.678/0001-00",
                Nome = "Empresa BancoBR.Net",
                Endereco = "Rua Teste BancoBR.Net",
                NumeroEndereco = "567",
                ComplementoEndereco = "Compl. End.",
                Bairro = "Centro",
                CEP = 12345678,
                Cidade = "Ribeirão Preto",
                UF = "SP",
                Convenio = "",
                NumeroAgencia = 825,
                DVAgencia = "0",
                NumeroConta = 12345,
                DVConta = "6"
            };

            var cnab = new ArquivoCNAB(BancoEnum.BradescoSA, empresa, numeroArquivo);

            Assert.Equal(237, cnab.Banco.Codigo);

            var lote = cnab.NovoLotePagamento(tipoServico, TipoLancamentoEnum.DebitoContaCorrente, FormaPagamentoEnum.CreditoConta);

            var pagamento1 = new Pagamento
            {
                PessoaEmpresaDestino = new Pessoa
                {
                    TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF,
                    CPF_CNPJ = "123.456.789-00",
                    Nome = "Colaborador A BancoBR.Net",
                    Endereco = "Rua Teste Colaborador A BancoBR.Net",
                    NumeroEndereco = "765",
                    ComplementoEndereco = "Compl.Colab A",
                    Bairro = "Bairro A",
                    CEP = 7654321,
                    Cidade = "São Paulo",
                    UF = "SP",
                    Banco = 341,
                    NumeroAgencia = 528,
                    DVAgencia = "0",
                    NumeroConta = 54321,
                    DVConta = "8"
                },
                TipoMovimento = TipoMovimentoEnum.Inclusao, //Valor Padrão, pode ser ignorado a setagem desta propriedade
                CodigoInstrucao = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado, //Valor Padrão, pode ser ignorado a setagem desta propriedade
                FinalidadeLancamento = "01", //Verificar nota P005 e P011 do CNAB240 FEBRABAN
                NumeroDocumento = "5637",
                DataPagamento = DateTime.Parse("2023-02-28"),
                Moeda = "BRL", //Valor Padrão, pode ser ignorado a setagem desta propriedade
                ValorPagamento = (decimal)2500.65
            };

            lote.NovoPagamento(pagamento1);

            var pagamento2 = new Pagamento
            {
                PessoaEmpresaDestino = new Pessoa
                {
                    TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF,
                    CPF_CNPJ = "123.456.789-00",
                    Nome = "Colaborador B BancoBR.Net",
                    Endereco = "Rua Teste Colaborador B BancoBR.Net",
                    NumeroEndereco = "765",
                    ComplementoEndereco = "Compl.Colab B",
                    Bairro = "Bairro B",
                    CEP = 98765432,
                    Cidade = "São Paulo",
                    UF = "SP",
                    Convenio = "",
                    NumeroAgencia = 135,
                    DVAgencia = "0",
                    NumeroConta = 98765,
                    DVConta = "7"
                },
                NumeroDocumento = "6598",
                DataPagamento = DateTime.Parse("2023-02-28"),
                ValorPagamento = (decimal)1830.34
            };
            lote.NovoPagamento(pagamento2);

            Assert.Equal(1, cnab.Trailer.QuantidadeLotes);
            Assert.Equal(4, lote.Trailer.QuantidadeRegistros);
            Assert.Equal(6, cnab.Trailer.QuantidadeRegistros);

            var stringArquivo = cnab.Exportar();

            //File.WriteAllText("c:\\cnabteste.txt", stringArquivo);

            foreach(var linha in stringArquivo.Split("\r\n"))
                Assert.Equal(240, linha.Length);
        }
    }
}

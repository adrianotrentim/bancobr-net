using BancoBr.CNAB;
using BancoBr.CNAB.Core;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Xunit;

namespace BancoBr.Tests
{
    public class CriaBancoTest
    {
        /// <summary>
        /// Para novos testes, apenas adicionar as linhas de cada banco no retorno da própriedade
        /// </summary>
        public static IEnumerable<object[]> CNAB240
        {
            get
            {
                var numeroArquivo = 1;

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

                return new[]
                {
                    new object[] { new ArquivoCNAB(BancoEnum.BradescoSA, empresa, numeroArquivo) },
                    //new object[] { new ArquivoCNAB(BancoEnum.Itau, empresa, numeroArquivo) }
                };
            }
        }

        /// <summary>
        /// Todas as informações podem ser adicionadas com formatações e acentos, pois a biblioteca cuidará de retira-los
        /// Todas as informações podem ser adicionadas em minúsculo, pois a biblioteca formatará em maiúsculo
        ///
        /// No caso de CEP, é um inteiro. A biblioteca cuidará para adicionar zeros a esquerda se necessário
        /// </summary>
        [Theory, MemberData("CNAB240")]
        public static void TestePagamentoTransferencia_e_Salario(ArquivoCNAB cnab)
        {
            var tipoServico = TipoServicoEnum.PagamentoSalarios;
            var lote = cnab.NovoLote(tipoServico, TipoLancamentoEnum.DebitoContaCorrente, FormaLancamentoEnum.CreditoContaMesmoBanco);

            var movimento1 = new Movimento
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
                DataPagamento = DateTime.Parse("2023-04-28"),
                Moeda = "BRL", //Valor Padrão, pode ser ignorado a setagem desta propriedade
                ValorPagamento = (decimal)2500.65
            };

            lote.NovoMovimento(movimento1);

            var movimento2 = new Movimento
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
                DataPagamento = DateTime.Parse("2023-04-28"),
                ValorPagamento = (decimal)1830.34
            };
            lote.NovoMovimento(movimento2);

            var stringArquivo = cnab.Exportar();
            //File.WriteAllText($"c:\\cnab240_{snab.Banco.Codigo}.txt", stringArquivo);

            #region ::. Testes Básicos .::

            //Todas as linhas devem conter exatamente 240 caracteres
            foreach (var linha in stringArquivo.Split("\r\n"))
                Assert.Equal(240, linha.Length);

            #endregion

            var fileName = Path.GetTempFileName();
            File.WriteAllText(fileName, stringArquivo);
        }

        [Theory, MemberData("CNAB240")]
        public static void TesteImportacaoRetornoPagamentoTransferencia_e_Salario(ArquivoCNAB cnab)
        {
            #region ::. Exportando e Importando o Arquivo

            var fileName = Path.GetTempFileName();

            var linhas = File.ReadLines(fileName);

            var cnabLeitura = new ArquivoCNAB((BancoEnum)cnab.Banco.Codigo, cnab.EmpresaCedente, 0);
            cnabLeitura.Importar(linhas);

            //Assert.Equal(((HeaderArquivo)cnab.Header).NumeroConta, ((HeaderArquivo)cnabLeitura.Header).NumeroConta);
            //Assert.Equal(((HeaderArquivo)cnab.Header).NomeEmpresa.RemoveAccents().ToUpper().Trim(), ((HeaderArquivo)cnabLeitura.Header).NomeEmpresa.Trim());

            //Assert.Equal(((HeaderLote)cnab.Lotes[0].Header).NumeroConta, ((HeaderLote)cnabLeitura.Lotes[0].Header).NumeroConta);
            //Assert.Equal(((HeaderLote)cnab.Lotes[0].Header).NomeEmpresa.RemoveAccents().ToUpper(), ((HeaderLote)cnabLeitura.Lotes[0].Header).NomeEmpresa.Trim());

            //Assert.Equal(((SegmentoA)cnab.Lotes[0].Detalhe[0]).ContaFavorecido, ((SegmentoA)cnabLeitura.Lotes[0].Detalhe[0]).ContaFavorecido);
            //Assert.Equal(((SegmentoA)cnab.Lotes[0].Detalhe[0]).NomeFavorecido.RemoveAccents().ToUpper(), ((SegmentoA)cnabLeitura.Lotes[0].Detalhe[0]).NomeFavorecido.Trim());
            //Assert.Equal(((SegmentoA)cnab.Lotes[0].Detalhe[0]).ValorPagamento, ((SegmentoA)cnabLeitura.Lotes[0].Detalhe[0]).ValorPagamento);

            //Assert.Equal(((TrailerLote)cnab.Lotes[0].Trailer).QuantidadeRegistros, ((TrailerLote)cnabLeitura.Lotes[0].Trailer).QuantidadeRegistros);
            //Assert.Equal(((TrailerArquivo)cnab.Trailer).QuantidadeRegistros, ((TrailerArquivo)cnabLeitura.Trailer).QuantidadeRegistros);

            //File.Delete(fileName);

            #endregion
        }
    }
}

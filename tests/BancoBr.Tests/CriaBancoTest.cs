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

                var empresa = new Correntista()
                {
                    TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ,
                    CPF_CNPJ = "12.345.678/0001-00",
                    Nome = "Correntista BancoBR.Net",
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
                    new object[] { BancoEnum.BradescoSA, empresa, numeroArquivo, LocalDebitoEnum.DebitoContaCorrente, TipoServicoEnum.PagamentoFornecedor },
                    //new object[] { new ArquivoCNAB(BancoEnum.Itau, empresa, numeroArquivo) }
                };
            }
        }

        #region ::. Criação dos Movimentos .::

        /// <summary>
        /// Todas as informações podem ser adicionadas com formatações e acentos, pois a biblioteca cuidará de retira-los
        /// Todas as informações podem ser adicionadas em minúsculo, pois a biblioteca formatará em maiúsculo
        ///
        /// No caso de CEP, é um inteiro. A biblioteca cuidará para adicionar zeros a esquerda se necessário
        /// </summary>
        public static List<Movimento> CriarMovimentoTransferenciaTED()
        {
            return new List<Movimento>
            {
                new Movimento
                {
                    Favorecido = new Favorecido
                    {
                        TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF,
                        CPF_CNPJ = "123.456.789-00",
                        Nome = "Fornecedor A BancoBR.Net",
                        Endereco = "Rua Teste Fornecedor A BancoBR.Net",
                        NumeroEndereco = "765",
                        ComplementoEndereco = "Compl.Fornec. A",
                        Bairro = "Bairro A",
                        CEP = 7654321,
                        Cidade = "São Paulo",
                        UF = "SP"
                    },
                    TipoLancamento = TipoLancamentoEnum.TEDOutraTitularidade,
                    TipoMovimento = TipoMovimentoEnum.Inclusao, //Valor Padrão, pode ser ignorado a setagem desta propriedade
                    CodigoInstrucao = CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado, //Valor Padrão, pode ser ignorado a setagem desta propriedade
                    NumeroDocumento = "5637",
                    DataPagamento = DateTime.Parse("2023-04-28"),
                    ValorPagamento = (decimal)2500.65,
                    Moeda = "BRL", //Valor Padrão, pode ser ignorado a setagem desta propriedade
                    MovimentoItem = new MovimentoItemTransferenciaTED
                    {
                        CodigoFinalidadeTED = FinalidadeTEDEnum.CreditoEmConta,
                        Banco = 341,
                        NumeroAgencia = 528,
                        DVAgencia = "0",
                        NumeroConta = 54321,
                        DVConta = "8"
                    }
                }
            };
        }

        /// <summary>
        /// Todas as informações podem ser adicionadas com formatações e acentos, pois a biblioteca cuidará de retira-los
        /// Todas as informações podem ser adicionadas em minúsculo, pois a biblioteca formatará em maiúsculo
        ///
        /// No caso de CEP, é um inteiro. A biblioteca cuidará para adicionar zeros a esquerda se necessário
        /// </summary>
        public static List<Movimento> CriarMovimentoTransferenciaPIX()
        {
            return new List<Movimento>
            {
                new Movimento
                {
                    Favorecido = new Favorecido()
                    {
                        TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF,
                        CPF_CNPJ = "123.456.789-00",
                        Nome = "Fornecedor B BancoBR.Net",
                        Endereco = "Rua Teste Fornecedor B BancoBR.Net",
                        NumeroEndereco = "765",
                        ComplementoEndereco = "Compl.Fornec. B",
                        Bairro = "Bairro B",
                        CEP = 98765432,
                        Cidade = "São Paulo",
                        UF = "SP",

                    },
                    TipoLancamento = TipoLancamentoEnum.PIXTransferencia,
                    NumeroDocumento = "6598",
                    DataPagamento = DateTime.Parse("2023-04-28"),
                    ValorPagamento = (decimal)1830.34,
                    MovimentoItem = new MovimentoItemTransferenciaPIX
                    {
                        TipoChavePIX = FormaIniciacaoEnum.PIX_Email,
                        ChavePIX = "adriano@divsoft.com.br"
                    }
                }
            };
        }

        #endregion

        [Theory, MemberData("CNAB240")]
        public static void CriarCNAB240(BancoEnum banco, Correntista correntista, int numeroArquivo, LocalDebitoEnum localDebito, TipoServicoEnum tipoServico)
        {
            var movimentos = new List<Movimento>();
            movimentos.AddRange(CriarMovimentoTransferenciaTED());

            var cnab = new ArquivoCNAB(banco, correntista, numeroArquivo, localDebito, tipoServico, movimentos);
            var stringArquivo = cnab.Exportar();

            var path = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\ArquivosGerados";
            File.WriteAllText(Path.Combine(path, $"cnab240_{banco}_TED.txt"), stringArquivo);

            #region ::. Testes Básicos .::

            //Todas as linhas devem conter exatamente 240 caracteres
            foreach (var linha in stringArquivo.Split("\r\n"))
            {
                if (string.IsNullOrWhiteSpace(linha))
                    continue; // Fim do Arquivo deve ter uma linha em branco

                Assert.Equal(240, linha.Length);
            }

            #endregion

            movimentos = new List<Movimento>();
            movimentos.AddRange(CriarMovimentoTransferenciaPIX());

            cnab = new ArquivoCNAB(banco, correntista, numeroArquivo, localDebito, tipoServico, movimentos);
            stringArquivo = cnab.Exportar();

            path = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\ArquivosGerados";
            File.WriteAllText(Path.Combine(path, $"cnab240_{banco}_PIX.txt"), stringArquivo);

            #region ::. Testes Básicos .::

            //Todas as linhas devem conter exatamente 240 caracteres
            foreach (var linha in stringArquivo.Split("\r\n"))
            {
                if (string.IsNullOrWhiteSpace(linha))
                    continue; // Fim do Arquivo deve ter uma linha em branco

                Assert.Equal(240, linha.Length);
            }

            #endregion
        }

        [Theory, MemberData("CNAB240")]
        public static void ImportacaoRetorno(BancoEnum banco, Correntista correntista, int numeroArquivo, LocalDebitoEnum localDebito, TipoServicoEnum tipoServico)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\ArquivosGerados";
            var fileName = Path.Combine(path, $"cnab240_{banco}_TED.txt");

            var linhas = File.ReadLines(fileName);

            var cnabLeitura = new ArquivoCNAB(banco, correntista);
            cnabLeitura.Importar(linhas);

            File.Delete(fileName);

            path = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\..\\ArquivosGerados";
            fileName = Path.Combine(path, $"cnab240_{banco}_PIX.txt");

            linhas = File.ReadLines(fileName);

            cnabLeitura = new ArquivoCNAB(banco, correntista);
            cnabLeitura.Importar(linhas);

            File.Delete(fileName);
        }
    }
}

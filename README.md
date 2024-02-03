# BancoBr.NET [![GitHub contributors](https://img.shields.io/github/contributors/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/graphs/contributors) [![GitHub issues](https://img.shields.io/github/issues/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/issues) [![GitHub issues-pr](https://img.shields.io/github/issues-pr/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/pulls) [![GitHub](https://img.shields.io/github/license/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/blob/main/LICENSE)

Biblioteca para integração bancária para pagamentos de contas, transferências e PIX.

## Situação do Projeto

![Alt](https://repobeats.axiom.co/api/embed/0a24518c7999f1499a1c8ffa0ae20835db99ba22.svg "Situação do Projeto")

## TODO

- [x] Geração de remessa padrão CNAB 240
- [x] Leitura de retorno padrão CNAB 240
- [ ] Transformação da leitura do arquivo de retorno em uma lista de Movimento
- [ ] Integração via API

## Segmentos

###### Transferência através de TED e PIX

- [x] Segmento A
- [x] Segmento B

###### Pagamento de Títulos de Cobrança - Boletos

- [x] Segmento J
- [x] Segmento J-52 - Código de Barras
- [ ] Segmento J-52 - PIX QRCode

###### Pagamento de Convênios (Luz, Água, Telefone...) e Tributos com Código de Barras

- [ ] Segmento O
- [ ] Segmento W

###### Pagamento de Tributos sem Código de Barras

- [ ] Segmento N
- [ ] Segmento B
- [ ] Segmento W

## Instituições

- [x] 237 - Bradesco
- [ ] 341 - Itaú
- [ ] 033 - Santander
- [ ] 756 - Sicoob
- [ ] 748 - Sicreedi
- [ ] 001 - Banco do Brasil
- [ ] 104 - Caixa Econômica

## Dúvidas?

Abra um issue na página do projeto no GitHub ou [clique aqui](https://github.com/adrianotentim/bancobr-net/issues).

## Exemplos

###### Criando uma remessa

```
var numeroArquivo = 1;

var correntista = new Correntista()
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

var movimentos = new List<Movimento>
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
      },
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
              ChavePIX = "nome@dominio.com.br"
          }
      }
  };

var cnab = new ArquivoCNAB(BancoEnum.BradescoS, correntista, numeroArquivo, LocalDebitoEnum.DebitoContaCorrente, TipoServicoEnum.PagamentoFornecedor, movimentos);
var stringArquivo = cnab.Exportar();
File.WriteAllText(Path.Combine("C:\\Teste", $"cnab240_237.txt"), stringArquivo);

```

###### Lendo um retorno

```
var correntista = new Correntista()
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

var fileName = Path.Combine("C:\\Teste", $"cnab240_237.txt");
var linhas = File.ReadLines(fileName);

var cnabLeitura = new ArquivoCNAB(BancoEnum.BradescoS, correntista);
cnabLeitura.Importar(linhas);

var dadosHeaderArquivo = (HeaderArquivo)cnab.Header;

foreach (var lote in (HeaderLote)cnab.Lotes) {
    foreach (var detalhe in lote.Detalhe) {
        if (detalhe is SegmentoA_Transferencia as segmentoA_Transferencia){
            var bancoFavorecido = segmentoA_Transferencia.BancoFavorecido;
            ......
        }

        if (detalhe is SegmentoB_Transferencia as segmentoB_Transferencia){
            var valorDocumento = segmentoB_Transferencia.ValorDocumento;
            ......
        }

        if (detalhe is SegmentoB_PIX as segmentoB_PIX){
            var chavePIX = segmentoB_PIX.ChavePIX;
            ......
        }
    }
}

```

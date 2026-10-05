# BancoBr.NET [![GitHub contributors](https://img.shields.io/github/contributors/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/graphs/contributors) [![GitHub issues](https://img.shields.io/github/issues/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/issues) [![GitHub issues-pr](https://img.shields.io/github/issues-pr/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/pulls) [![GitHub](https://img.shields.io/github/license/adrianotrentim/bancobr-net)](https://github.com/adrianotrentim/bancobr-net/blob/main/LICENSE)

Biblioteca para integração bancária para pagamentos de contas, transferências e PIX.

## Estatísticas do Projeto

![Alt](https://repobeats.axiom.co/api/embed/0a24518c7999f1499a1c8ffa0ae20835db99ba22.svg "Situação do Projeto")

## TODO

- [x] Geração de remessa padrão CNAB 240
- [x] Leitura de retorno padrão CNAB 240
- [ ] Integração via API
  - [x] Sicoob
  - [ ] Demais instituições

## Segmentos

###### Transferência através de TED e PIX

- [x] Segmento A
- [x] Segmento B

###### Pagamento de Títulos de Cobrança - Boletos

- [x] Segmento J
- [x] Segmento J-52 - Código de Barras
- [x] Segmento J-52 - PIX QRCode

###### Pagamento de Convênios (Luz, Água, Telefone...) e Tributos com Código de Barras

- [x] Segmento O
- [ ] Segmento W

###### Pagamento de Tributos sem Código de Barras

- [ ] Segmento N
- [ ] Segmento B
- [ ] Segmento W

## Instituições

- [x] 237 - Bradesco
- [x] 341 - Itaú
- [x] 033 - Santander
- [ ] 756 - Sicoob
- [x] 748 - Sicredi
- [ ] 001 - Banco do Brasil
- [x] 104 - Caixa Econômica
- [x] 077 - Inter

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

foreach (var movimento in cnab.Movimentos) {
    ......
}

```

## Integração via API (Sicoob)

Além da geração/leitura de arquivos CNAB, a `BancoBr.API` consome diretamente a API do Sicoob
(Pix, Boleto, Convênio, TED e Conta Corrente), usando o mesmo contrato `Movimento`/`MovimentoItem`
já usado pelo CNAB. O ponto de entrada único é `BancoApi.Conectar`, que devolve um cliente pronto
para todas as operações — sem o chamador precisar escolher qual cliente interno instanciar.

A autenticação do Sicoob exige certificado digital ICP-Brasil A1 (mTLS) além do OAuth2. Instale o
pacote `BancoBr.API` e forneça o certificado via `CertificateSource`:

```
dotnet add package BancoBr.API
```

###### Conectando

```csharp
using BancoBr.API;
using BancoBr.API.Core.Http;
using BancoBr.Common.Enums;

var certificateSource = CertificateSource.FromPfxFile("caminho/para/certificado.pfx", "senhaDoCertificado");

// Fluxo padrão: OAuth2 client_credentials, renovado automaticamente por baixo.
var banco = BancoApi.Conectar(BancoEnum.Sicoob, clientId: "seu-client-id", clientSecret: "seu-client-secret", certificateSource);

// Alternativa: token já emitido (ex.: portal de sandbox do Sicoob), pulando o fluxo OAuth2.
// var banco = BancoApi.Conectar(BancoEnum.Sicoob, clientId: "seu-client-id", certificateSource, new StaticAccessTokenProvider("token-bearer"));
```

###### Pagando um boleto (consulta prévia + pagamento)

```csharp
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

var origem = new Correntista
{
    NumeroAgencia = 4321,
    NumeroConta = 12345,
    TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ,
    CPF_CNPJ = "12.345.678/0001-00"
};

var movimento = new Movimento
{
    TipoLancamento = TipoLancamentoEnum.PagamentoTituloOutroBanco,
    MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = "00190500954014481606906809350314337370000000100" }
};

// Consulta prévia: preenche o boleto (valor, vencimento, beneficiário) e o IdentificadorConsulta.
await banco.ConsultarAgendamentoAsync(movimento, origem);

var item = (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem;
Console.WriteLine($"Beneficiário: {movimento.Favorecido?.Nome}, Valor: {item.ValorCodigoBarra:C}");

// Envio: como a consulta prévia já deixou IdentificadorConsulta preenchido, paga direto com esses
// dados (sem consultar de novo).
await banco.EnviarAgendamentoAsync(movimento, origem, idempotencyKey: Guid.NewGuid().ToString());

Console.WriteLine($"Situação={movimento.SituacaoBancoBr}, IdPagamento={movimento.NumeroDocumentoNoBanco}");
```

###### Pix (transferência por chave)

```csharp
var movimentoPix = new Movimento
{
    TipoLancamento = TipoLancamentoEnum.PIXTransferencia,
    ValorPagamento = 150.00m,
    MovimentoItem = new MovimentoItemTransferenciaPIX
    {
        TipoChavePIX = FormaIniciacaoEnum.PIX_Email,
        ChavePIX = "fornecedor@dominio.com.br"
    }
};

// EnviarAgendamentoAsync já inicia e confirma o pagamento numa única chamada.
await banco.EnviarAgendamentoAsync(movimentoPix, origem);

Console.WriteLine($"Situação={movimentoPix.SituacaoBancoBr}, EndToEndId={movimentoPix.NumeroDocumentoNoBanco}");
```

###### TED

```csharp
var movimentoTed = new Movimento
{
    TipoLancamento = TipoLancamentoEnum.TEDOutraTitularidade,
    ValorPagamento = 2500.65m,
    Favorecido = new Favorecido
    {
        TipoPessoa = TipoInscricaoCPFCNPJEnum.CPF,
        CPF_CNPJ = "123.456.789-00",
        Nome = "Fornecedor A"
    },
    MovimentoItem = new MovimentoItemTransferenciaTED
    {
        CodigoFinalidadeTED = FinalidadeTEDEnum.CreditoEmConta,
        Banco = 341,
        NumeroAgencia = 528,
        DVAgencia = "0",
        NumeroConta = 54321,
        DVConta = "8"
    }
};

await banco.EnviarAgendamentoAsync(movimentoTed, origem);
await banco.ConsultarAgendamentoAsync(movimentoTed, origem); // situação/comprovante
// await banco.CancelarAgendamentoAsync(movimentoTed, origem); // enquanto agendada
```

###### Consultando o DDA (boletos a pagar)

```csharp
var boletosDda = await banco.ConsultarDDAAsync(
    numeroConta: 12345,
    dataInicial: DateTime.Today,
    dataFinal: DateTime.Today.AddDays(30),
    situacao: SituacaoBoletoEnum.EmAberto,
    tipoData: TipoDataConsultaEnum.Vencimento);

foreach (var movimento in boletosDda)
{
    var dda = (MovimentoItemDDA)movimento.MovimentoItem;
    Console.WriteLine($"{dda.NomeRazaoSocialBeneficiario}: {dda.ValorBoleto:C} - vencimento {dda.DataVencimentoBoleto:d}");
}
```

###### Consultando o extrato da conta corrente

```csharp
var extrato = await banco.ConsultarExtratoAsync(numeroContaCorrente: 12345, mes: 9, ano: 2026);

// O primeiro item é sempre o saldo do período.
var saldo = (MovimentoItemExtratoSaldo)extrato[0].MovimentoItem;
Console.WriteLine($"Saldo atual: {saldo.SaldoAtual:C}");

foreach (var movimento in extrato.Skip(1))
{
    var transacao = (MovimentoItemExtratoTransacao)movimento.MovimentoItem;
    Console.WriteLine($"{transacao.Data:d} [{transacao.Tipo}] {transacao.Descricao}: {transacao.Valor:C}");
}
```

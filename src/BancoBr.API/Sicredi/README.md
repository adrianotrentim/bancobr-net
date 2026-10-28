# Sicredi — Multipag, extrato e saldo

Implementação baseada nos guias e referências OpenAPI oficiais consultados em 28/10/2026:

- [Multipag](https://developers.sicredi.com.br/public/docs/getting-started-multipag), [boletos](https://developers.sicredi.com.br/public/docs/guia-de-pagamento-de-boletos), [tributos](https://developers.sicredi.com.br/public/docs/guia-de-pagamentos-de-tributos), [Pix](https://developers.sicredi.com.br/public/docs/guia-de-pagamentos-pix).
- [Aprovação digital](https://developers.sicredi.com.br/public/docs/aprova%C3%A7%C3%A3o-digital), [situações](https://developers.sicredi.com.br/public/docs/situa%C3%A7%C3%A3o-de-pagamento), [PDF](https://developers.sicredi.com.br/public/docs/comprovantes), [DDA](https://developers.sicredi.com.br/public/docs/dda).
- [Extrato](https://developers.sicredi.com.br/public/docs/getting-started-current-account), [referência](https://developers.sicredi.com.br/public/reference/consultarextrato).
- [Saldo](https://developers.sicredi.com.br/public/docs/getting-started-account-balance), [referência](https://developers.sicredi.com.br/public/reference/consultarsaldo).

## Operações disponíveis

A fachada `BancoApi` suporta envio, consulta e cancelamento de boleto, tributo/conta de consumo com código de barras e Pix por chave; DDA; extrato paginado; saldo atual; download binário do comprovante. TED, Pix por dados bancários, QR Code e cadastro de webhook não estão implementados nesta integração. O webhook oficial tem somente ambiente de produção.

O Sicredi não oferece consulta prévia de boleto ou tributo por código, nem resolução prévia de chave Pix. Informe valor, data e `NumeroDocumento` (identificador do associado, até 100 caracteres) antes de enviar. Código de barras deve conter 44 dígitos; converta linhas digitáveis antes de chamar. Para Pix, informe também `Favorecido.CPF_CNPJ` e preferencialmente `Favorecido.Nome`. Para FGTS, informe `IdentificadorFgts = "0181"` no item de convênio.

`RECEBIDO`, `AGENDADO`, `AGUARDANDO_APROVACAO` e `APROVADO` são normalizados como `Agendado`, nunca como liquidação. Somente `SUCESSO` é `Efetivado`. `SituacaoOriginalBancoBr` preserva o status original. A aprovação é realizada nos canais digitais do Sicredi.

`IdRequisicaoBancoBr` guarda o `idTransacao` antes do envio; `NumeroDocumentoNoBanco` recebe esse mesmo valor após resposta de criação aceita. O UUID interno do Sicredi e o endToEnd Pix não são usados como chave de consulta. Persista a identidade de cada lançamento antes de enviar e reutilize-a em caso de timeout; consulte pelo `idTransacao` para reconciliar o resultado antes de reenviar. A biblioteca não presume garantia de deduplicação do servidor nem repete pagamentos após erros de transporte/5xx. Somente HTTP 401 causa uma renovação e repetição, mantendo o mesmo corpo.

```csharp
var banco = BancoApi.Conectar(new ConfiguracaoAPI
{
    ClientId = clientId,
    ClientSecret = clientSecret,
    CertificadoDigital = CertificateSource.FromPfxFile(caminhoPfx, senhaPfx),
    AmbienteHomologacao = true,
    Correntista = new Correntista
    {
        Banco = (int)BancoEnum.Sicredi,
        NumeroAgencia = 100, NumeroConta = 0, DVConta = "1",
        CPF_CNPJ = "11111111000111"
    }
});

var movimento = new Movimento
{
    NumeroDocumento = "ERP-0001",
    IdRequisicaoBancoBr = "identidade-persistida-do-lancamento",
    DataPagamento = new DateTime(2026, 10, 28),
    ValorPagamento = 6000m,
    MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra
    {
        CodigoBarras = "74892950500006000001123100010108100200003107"
    }
};
await banco.EnviarAgendamentoAsync(movimento);
await banco.ConsultarAgendamentoAsync(movimento);
// Apenas após SUCESSO:
// byte[] pdf = await banco.BaixarComprovanteAsync(movimento);
// await banco.CancelarAgendamentoAsync(movimento); // enquanto cancelável

var extrato = await banco.ConsultarExtratoAsync(0, 6, 2026);
var saldoAtual = await banco.ConsultarSaldoAsync();
```

As contas de extrato/saldo são vinculadas à credencial; o parâmetro `numeroContaCorrente` da fachada é ignorado. O extrato inclui um `MovimentoItemExtratoSaldo` inicial com saldo anterior e saldo final do período, seguido dos lançamentos com valores absolutos, direção crédito/débito e `SaldoAposLancamento`. Esse saldo final não é o saldo atual em tempo real; use `ConsultarSaldoAsync` para este último. Contas legadas podem apresentar movimentos em D-1. DDA suporta somente boletos em aberto e filtro por vencimento; sua base de homologação é `multipag-sandbox`, diferente de `multipag-pagamento-sandbox` usada nos pagamentos.

## Liberação junto ao Sicredi

1. Solicite à cooperativa adesão ao Multipag e às APIs de Extrato e Saldo. Multipag está descrito como piloto; credenciais de homologação são fornecidas pela equipe de integração.
2. Para Multipag, o usuário Master envia o CSR em Internet Banking → Outros Serviços → Acesso API Pix → Gerenciar Certificados e baixa certificado/cadeia após aprovação.
3. Para extrato/saldo, obtenha o ID de Adesão, cadastre o mesmo e-mail no [Portal do Desenvolvedor](https://sicredi.sensedia-devportal.com/pt/), abra chamado “Acesso à API Pix” com o ID de Adesão e registre o CSR em Certificados e Credenciais após a liberação do catálogo. Siga as orientações do e-mail de boas-vindas para as credenciais de sandbox e, depois dos testes, para solicitar produção.
4. Se já usa Multipag, a documentação permite reutilizar certificado e credenciais: solicite os escopos `contacorrente.extratos.consultar` e `contacorrente.saldo.consultar`. Pagamentos usam `multipag.boleto.pagar/consultar`, `multipag.tributos.pagar/consultar`, `multipag.pix.pagar/consultar`; DDA usa `multipag.dda.consultar` e exige adesão como Pagador Eletrônico no IB/app.
5. Converta certificado e chave privada para PFX para usar `CertificateSource` (netstandard2.0). Armazene arquivos e senhas fora do repositório.

## Smoke de homologação

```sh
dotnet run --project tests/BancoBr.Sicredi.Sandbox/BancoBr.Sicredi.Sandbox.csproj
```

Configure no ambiente: `SICREDI_CLIENT_ID`, `SICREDI_CLIENT_SECRET`, `SICREDI_CERT_PFX_PATH`, `SICREDI_CERT_PASSWORD` (opcional se PFX sem senha), `SICREDI_COOPERATIVA`, `SICREDI_CONTA` (sem DV), `SICREDI_DV_CONTA`, `SICREDI_DOCUMENTO`. Opcional: `SICREDI_DATA_EXTRATO` (`yyyy-MM-dd`, padrão 2026-06-01; consulta o mês inteiro) e `SICREDI_ID_TRANSACAO` (boleto de teste existente, fornecido pelo banco).

O console fixa homologação, consulta saldo/extrato e, se houver ID, consulta pagamento; não envia pagamentos. Código de saída 0 = consultas executadas passaram, 1 = falha, 2 = configuração ausente/comunicação não executada. Sem ID de transação, a comunicação Multipag é explicitamente marcada como não executada. Não imprime valores de credenciais, dados bancários ou corpos de resposta.

## Validação desta implementação

Compilação da solução aprovada e 162 testes locais aprovados, incluindo 21 cenários Sicredi com HTTP simulado. O console foi executado e retornou saída 2: credenciais, certificado e identificação da conta de homologação ausentes. Portanto, comunicação autenticada com o Sicredi e homologação bancária permanecem pendentes.

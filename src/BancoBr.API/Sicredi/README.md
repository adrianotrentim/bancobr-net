# Usar o Sicredi com BancoBr.NET

Esta integração permite pagar boletos, tributos e contas de consumo com código de barras, fazer Pix por chave ou dados bancários e consultar DDA, extrato e saldo.

**Situação atual:** a implementação foi validada com respostas simuladas. O teste de comunicação com o Sicredi ainda depende de credenciais e certificado de homologação.

## O que está disponível

| Operação | Suporte nesta integração |
| --- | --- |
| Boleto com código de barras | Enviar, consultar, cancelar agendamento e obter PDF do comprovante |
| Tributo ou conta de consumo com código de barras | Enviar, consultar, cancelar agendamento e obter PDF do comprovante |
| Pix por chave ou dados bancários | Enviar, consultar, cancelar agendamento e obter PDF do comprovante |
| DDA — boletos emitidos contra o pagador cadastrado | Consultar boletos em aberto por vencimento |
| Extrato | Consultar um período, incluindo todas as páginas |
| Saldo | Consultar o saldo atual |
| TED | Sem endpoint na documentação Multipag consultada; não suportado nesta integração por API |
| Pagamento por QR Code e cadastro de webhook | Não implementados nesta integração |

A documentação de pagamentos por API consultada descreve boletos, tributos e Pix. As referências a TED nos manuais CNAB tratam de integração por arquivo, que é um fluxo separado.

## Antes de conectar: o que pedir ao Sicredi

Solicite à sua cooperativa a liberação de **Multipag, Extrato e Saldo**. Além das credenciais, você vai precisar de um certificado que identifica sua aplicação nas conexões com o banco.

### Para pagamentos pelo Multipag

1. Solicite a adesão e assine o termo indicado pela cooperativa.
2. Peça as credenciais de homologação — **Client ID** e **Client Secret** — à equipe de integração. A documentação descreve o Multipag como uma solução em piloto.
3. Com um usuário **Master**, acesse Internet Banking → Outros Serviços → Acesso API Pix → Gerenciar Certificados.
4. Envie o **CSR**, que é o pedido de emissão do certificado. Após a aprovação, baixe o certificado e a cadeia de certificados.

### Para extrato e saldo

Se você já usa Multipag, a documentação permite reaproveitar certificado e credenciais. Solicite a liberação de acesso também às APIs de Extrato e Saldo.

Se ainda não tem esse acesso:

1. Solicite a adesão à cooperativa e aguarde o **ID de Adesão** enviado pelo Sicredi.
2. Crie sua conta no [Portal do Desenvolvedor](https://sicredi.sensedia-devportal.com/pt/) usando o mesmo e-mail informado na adesão.
3. Abra um chamado do tipo **“Acesso à API Pix”** e informe o ID de Adesão, conforme o procedimento descrito pelo banco para essas APIs.
4. Após a liberação, registre o CSR em **Certificados e Credenciais** e baixe os arquivos aprovados.
5. Siga o e-mail de boas-vindas para obter acesso aos testes. Depois de concluir os testes, solicite as credenciais de produção pelo canal indicado nesse e-mail.

Para usar `CertificateSource`, reúna o certificado e sua chave privada em um arquivo **PFX**. Guarde esse arquivo e as senhas fora do repositório.

## Conectar sua aplicação

Os dados abaixo são fictícios. Substitua-os pelos dados da conta liberada pelo Sicredi. `ClientId`, `ClientSecret`, o caminho do PFX e a senha devem vir da configuração segura da sua aplicação.

```csharp
using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

var banco = BancoApi.Conectar(new ConfiguracaoAPI
{
    ClientId = clientId,
    ClientSecret = clientSecret,
    CertificadoDigital = CertificateSource.FromPfxFile(caminhoPfx, senhaPfx),
    AmbienteHomologacao = true,
    Correntista = new Correntista
    {
        Banco = (int)BancoEnum.Sicredi,
        NumeroAgencia = 100,
        NumeroConta = 0,
        DVConta = "1",
        CPF_CNPJ = "11111111000111"
    }
});
```

`AmbienteHomologacao = true` seleciona o ambiente de testes. Use as credenciais correspondentes a esse ambiente.

## Enviar e acompanhar um boleto

No Sicredi, informe os dados completos e envie o pagamento. A integração não oferece consulta prévia do boleto pelo código de barras nem consulta prévia do titular de uma chave Pix.

Neste exemplo, o código de barras, a data, o valor e o identificador salvo vêm da sua aplicação:

```csharp
var movimento = new Movimento
{
    NumeroDocumento = "ERP-0001",
    DataPagamento = dataPagamento,
    ValorPagamento = valorPagamento,
    MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra
    {
        CodigoBarras = codigoBarras
    }
};

await banco.EnviarAgendamentoAsync(movimento, idempotencyKey: identificadorSalvoNoSeuSistema);
await banco.ConsultarAgendamentoAsync(movimento);

if (movimento.Situacao == BancoBrSituacaoEnum.Efetivado)
{
    Console.WriteLine("Pagamento realizado.");
}
```

O código de barras deve ter **44 dígitos**. Se sua aplicação recebe uma linha digitável, converta-a para código de barras antes da chamada. `NumeroDocumento` identifica o pagamento no seu sistema e aceita até 100 caracteres.

Crie e guarde o identificador do envio antes da primeira tentativa. Reutilize esse mesmo valor se precisar tentar novamente. Depois que o banco aceitar o envio, `NumeroDocumentoNoBanco` recebe a referência usada nas consultas seguintes.

Guarde também `NumeroDocumentoNoBanco` com o movimento. Se chamar `EnviarAgendamentoAsync` com essa referência preenchida, inclusive depois de reconstruir o movimento salvo, a biblioteca consulta o pagamento existente em vez de criar outro. Isso vale mesmo se outra chave for informada na chamada. Se a consulta falhar, a biblioteca informa o erro e não faz um novo envio automaticamente.

Se a conexão cair antes da resposta, verifique se o banco registrou a solicitação antes de reenviar. A biblioteca não repete automaticamente envios após falhas de conexão ou erros de servidor. Uma resposta HTTP 401 pode gerar uma renovação do token e uma nova tentativa com os mesmos dados.

### Entender a resposta do banco

O registro da solicitação é uma etapa do pagamento. Ele ainda pode depender de aprovação no aplicativo ou no Internet Banking.

| Resposta do Sicredi | O que significa |
| --- | --- |
| `RECEBIDO` | Solicitação recebida para processamento |
| `AGENDADO` | Pagamento aguardando a data informada |
| `AGUARDANDO_APROVACAO` | É necessário aprovar pelos canais do banco |
| `APROVADO` | Aprovado, aguardando a execução |
| `SUCESSO` | Pagamento efetivado |
| `CANCELADO`, `ERRO`, `NAO_APROVADO` ou `EXPIRADO` | O pagamento foi cancelado ou não foi concluído |

Os quatro primeiros estados aparecem em `Movimento.Situacao` como `Agendado`. Somente a confirmação de sucesso aparece como `Efetivado`. Acompanhe a aprovação pelos canais do Sicredi.

### Obter o PDF ou cancelar um agendamento

Quando o pagamento estiver efetivado:

```csharp
bool obtido = await banco.ConsultarComprovanteAsync(movimento);
if (obtido)
    System.IO.File.WriteAllBytes("comprovante.pdf", movimento.ArquivoComprovantePDF);
```

O PDF fica em `movimento.ArquivoComprovantePDF`, e o retorno `true` indica que ele foi obtido. A chamada e o retorno são iguais no Santander: links e download são tratados internamente. Se o pagamento ou o arquivo ainda não estiver pronto, o retorno é `false` e a propriedade fica nula; consulte novamente mais tarde.

Para solicitar o cancelamento de um agendamento ainda cancelável:

```csharp
await banco.CancelarAgendamentoAsync(movimento);
```

## Consultar saldo e extrato

```csharp
var saldo = await banco.ConsultarSaldoAsync();
var extrato = await banco.ConsultarExtratoAsync(0, mes: 10, ano: 2026);
```

O Sicredi consulta a conta vinculada às credenciais da conexão. Por isso, o primeiro argumento de `ConsultarExtratoAsync` pode ser `0`: ele é ignorado nessa integração.

O extrato traz um item inicial com o saldo anterior e o saldo final do período, seguido das transações. Para saber o saldo atual em tempo real, use `ConsultarSaldoAsync`.

Algumas contas legadas do Sicredi podem apresentar movimentos apenas no dia seguinte, em D-1.

## Particularidades de outras operações

- **Pix por chave:** informe também `Favorecido.CPF_CNPJ`. Preencha `Favorecido.Nome` sempre que possível para facilitar a conferência na aprovação.
- **Pix por dados bancários:** deixe `ChavePIX` vazia e informe ISPB, agência, conta, DV e tipo de conta no item Pix, além de nome e documento em `Favorecido`. O Sicredi exige ISPB; somente o código COMPE em `Banco` não é suficiente. A integração usa os tipos já existentes: conta corrente, poupança e pagamento.
- **FGTS:** informe `IdentificadorFgts = "0181"` no item de pagamento de convênio.
- **DDA:** exige adesão como Pagador Eletrônico no Internet Banking ou aplicativo. A consulta desta integração traz boletos em aberto, filtrados por vencimento.

Nas consultas de pagamentos, os campos que o banco devolver — como valor, data, código de barras, chave e beneficiário Pix — atualizam o movimento. Campos ausentes ou nulos preservam os dados já conhecidos.

### Exemplo de Pix por agência e conta

Use os mesmos modelos das outras integrações. Os dados abaixo são fictícios:

```csharp
var pix = new Movimento
{
    NumeroDocumento = "ERP-PIX-0001",
    DataPagamento = dataPagamento,
    ValorPagamento = valorPagamento,
    Favorecido = new Favorecido
    {
        Nome = "Favorecido de exemplo",
        CPF_CNPJ = "11111111111"
    },
    MovimentoItem = new MovimentoItemTransferenciaPIX
    {
        Ispb = "91586982",
        NumeroAgencia = 101,
        NumeroConta = 1111,
        DVConta = "9",
        TipoConta = TipoContaEnum.ContaCorrente
    }
};

await banco.EnviarAgendamentoAsync(pix, idempotencyKey: identificadorSalvoNoSeuSistema);
await banco.ConsultarAgendamentoAsync(pix);
```

Se `ChavePIX` estiver preenchida, o envio será por chave. Consulta, cancelamento e PDF usam os mesmos métodos para as duas modalidades Pix.

## Testar a conexão com o Sicredi

O projeto `BancoBr.Sicredi.Sandbox` faz um teste rápido de comunicação em homologação. Ele consulta saldo e extrato. Se você informar um ID de transação de teste, também consulta esse pagamento. **Esse programa não envia pagamentos.**

### 1. Configurar os dados do teste

Defina estas variáveis de ambiente na máquina que vai executar o programa:

| Variável | O que informar |
| --- | --- |
| `SICREDI_CLIENT_ID` | Client ID de homologação |
| `SICREDI_CLIENT_SECRET` | Client Secret de homologação |
| `SICREDI_CERT_PFX_PATH` | Caminho do arquivo PFX |
| `SICREDI_COOPERATIVA` | Número da cooperativa |
| `SICREDI_CONTA` | Número da conta, sem o dígito verificador |
| `SICREDI_DV_CONTA` | Dígito verificador da conta |
| `SICREDI_DOCUMENTO` | CPF ou CNPJ do titular |

Você também pode informar:

| Variável opcional | Quando usar |
| --- | --- |
| `SICREDI_CERT_PASSWORD` | Quando o PFX tiver senha |
| `SICREDI_DATA_EXTRATO` | Para escolher o mês do extrato; use uma data no formato `yyyy-MM-dd`. O padrão é junho de 2026 |
| `SICREDI_ID_TRANSACAO` | Para consultar um boleto de teste já existente, fornecido pelo Sicredi |

### 2. Executar o teste

Na raiz do repositório:

```sh
dotnet run --project tests/BancoBr.Sicredi.Sandbox/BancoBr.Sicredi.Sandbox.csproj
```

### 3. Conferir o resultado

| Código de saída | Resultado |
| --- | --- |
| `0` | As consultas executadas passaram |
| `1` | Houve uma falha de configuração ou comunicação, ou o banco rejeitou a consulta |
| `2` | Faltam configurações; a comunicação não foi executada |

Sem `SICREDI_ID_TRANSACAO`, o programa informa que não consultou o Multipag. Nesse caso, um resultado positivo de saldo e extrato não valida a comunicação de pagamentos.

Na execução realizada durante a implementação, o programa retornou `2`: faltavam credenciais, certificado e identificação da conta. A comunicação autenticada e a homologação bancária continuam pendentes.

## Referência técnica

Os detalhes abaixo ajudam a configurar permissões e investigar a comunicação.

- **Segurança:** as chamadas usam certificado na conexão, chamado mTLS, e um token de acesso OAuth2.
- **Identificador de acompanhamento:** o `idTransacao` vem do identificador informado no envio. O UUID interno do Sicredi e o `endToEnd` Pix não são usados como chave de consulta nesta integração.
- **Ambiente de testes:** pagamentos usam a base `multipag-pagamento-sandbox`; DDA usa `multipag-sandbox`.
- **Datas:** são enviadas como `yyyy-MM-dd`, independentemente da configuração regional da aplicação.
- **Webhook:** o cadastro não está implementado. A documentação oficial disponibiliza esse recurso somente em produção.

As permissões de acesso, chamadas *escopos*, devem estar liberadas nas credenciais:

| Uso | Escopos |
| --- | --- |
| Boletos | `multipag.boleto.pagar` e `multipag.boleto.consultar` |
| Tributos e contas de consumo | `multipag.tributos.pagar` e `multipag.tributos.consultar` |
| Pix | `multipag.pix.pagar` e `multipag.pix.consultar` |
| DDA | `multipag.dda.consultar` |
| Extrato | `contacorrente.extratos.consultar` |
| Saldo | `contacorrente.saldo.consultar` |

### Documentação oficial consultada

Fontes consultadas em 28/10/2026:

- [Multipag](https://developers.sicredi.com.br/public/docs/getting-started-multipag), [boletos](https://developers.sicredi.com.br/public/docs/guia-de-pagamento-de-boletos), [tributos](https://developers.sicredi.com.br/public/docs/guia-de-pagamentos-de-tributos) e [Pix](https://developers.sicredi.com.br/public/docs/guia-de-pagamentos-pix).
- [Aprovação digital](https://developers.sicredi.com.br/public/docs/aprova%C3%A7%C3%A3o-digital), [situações](https://developers.sicredi.com.br/public/docs/situa%C3%A7%C3%A3o-de-pagamento), [comprovantes](https://developers.sicredi.com.br/public/docs/comprovantes) e [DDA](https://developers.sicredi.com.br/public/docs/dda).
- [Guia de extrato](https://developers.sicredi.com.br/public/docs/getting-started-current-account) e [referência de extrato](https://developers.sicredi.com.br/public/reference/consultarextrato).
- [Guia de saldo](https://developers.sicredi.com.br/public/docs/getting-started-account-balance) e [referência de saldo](https://developers.sicredi.com.br/public/reference/consultarsaldo).

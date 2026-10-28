# Enviar e acompanhar pagamentos

Você pode usar o BancoBr.NET como já usa hoje ou escolher as chamadas detalhadas para receber mais informações. As duas formas funcionam com Sicoob, Santander, Banco do Brasil e Sicredi, respeitando as operações disponíveis em cada banco.

## Para quem já usa a biblioteca

Você pode continuar usando `Movimento`, `EnviarAgendamentoAsync` e `ConsultarAgendamentoAsync`. Os modelos do `BancoBr.Common`, os métodos existentes e os valores dos estados antigos foram preservados.

A forma detalhada é útil quando você precisa saber se falta autorização, guardar uma referência para consultar depois ou acessar informações adicionais enviadas pelo banco.

| Sua necessidade | Forma de uso |
| --- | --- |
| Manter a integração atual | Continue com os métodos existentes |
| Receber informações de execução e autorização separadamente | Use `EnviarPagamentoAsync` e `ConsultarPagamentoAsync` |
| Acessar comprovantes e informações adicionais do extrato | Use `ObterComprovanteAsync` e `ConsultarExtratoDetalhadoAsync` |

## Preparar um pagamento

Primeiro, [configure a conexão com o banco](../README.md#conectando). Nos exemplos abaixo, `banco` é essa conexão. O código de barras, a data e o favorecido vêm dos dados do boleto que sua aplicação vai pagar.

```csharp
using BancoBr.Common.Instances;

var movimento = new Movimento
{
    NumeroDocumento = "1234",
    DataPagamento = dataPagamento,
    ValorPagamento = 150m,
    Favorecido = favorecido,
    MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra
    {
        CodigoBarras = codigoBarras,
        ValorCodigoBarra = 150m
    }
};
```

Use os dados completos do pagamento. A biblioteca fará as consultas prévias que o banco exigir durante o envio.

O exemplo usa `NumeroDocumento` numérico para funcionar também no BB. Nesse banco, o número deve estar entre 1 e 9999999, ser diferente para cada nova solicitação e permanecer igual nas tentativas do mesmo pagamento.

### Enviar pela forma que você já conhece

Gere um identificador para o pagamento e guarde-o no seu sistema antes do primeiro envio. Na hora de enviar, leia esse mesmo identificador:

```csharp
Guid idOperacao = identificadorSalvoNoSeuSistema;

await banco.EnviarAgendamentoAsync(movimento, idOperacao.ToString());
await banco.ConsultarAgendamentoAsync(movimento);
```

Para continuar o acompanhamento em outra execução da aplicação, guarde também os dados do movimento e o identificador retornado em `NumeroDocumentoNoBanco`.

### Enviar e receber mais informações

Este é um exemplo alternativo ao envio acima. Escolha uma das duas formas para enviar o pagamento.

```csharp
using BancoBr.API.Operacoes;

Guid idOperacao = identificadorSalvoNoSeuSistema;

var resultado = await banco.EnviarPagamentoAsync(new RequisicaoPagamento
{
    IdOperacao = idOperacao,
    Movimento = movimento
});
```

O resultado contém o movimento, a situação do pagamento, a informação de autorização e uma referência para as próximas consultas. O envio é o mesmo ao trocar de banco; a configuração da conexão identifica qual instituição será usada.

## Saber se o pagamento já foi realizado

Enviar a solicitação e receber uma resposta do banco ainda pode deixar etapas pendentes. Por exemplo, uma empresa pode exigir que dois sócios autorizem o pagamento antes da execução.

O resultado detalhado separa essas informações:

| Informação | O que ela responde | Exemplos de valores |
| --- | --- | --- |
| `resultado.Situacao` | Em que etapa está o pagamento? | `EmProcessamento`, `Agendado`, `Efetivado` |
| `resultado.Autorizacao` | O banco informou se falta aprovação? | `AguardandoAutorizacao`, `Autorizado`, `NaoInformada` |

Use `Efetivado` para reconhecer que o pagamento foi realizado:

```csharp
if (resultado.Situacao == SituacaoPagamento.Efetivado)
{
    MarcarComoPago(idOperacao);
}
else if (resultado.Autorizacao == SituacaoAutorizacao.AguardandoAutorizacao)
{
    ExibirMensagem("Autorize o pagamento nos canais do banco.");
}
```

`NaoInformada` significa que o banco não forneceu informação de autorização naquela resposta. A biblioteca também não presume quantas assinaturas já foram feitas ou quantas faltam. A aprovação continua sendo feita pelos canais do banco.

Na chamada antiga, uma pendência pode continuar aparecendo como `Agendado` ou `NaoIntegrado`, conforme o comportamento que a integração já tinha. Os detalhes adicionais ficam no novo resultado.

## Guardar a referência e consultar depois

Pense na referência como o protocolo de acompanhamento do pagamento: guarde-a inteira e devolva-a à biblioteca quando precisar consultar. Você não precisa interpretar ou montar seus campos.

Este exemplo usa Newtonsoft.Json, já utilizado pela biblioteca:

```csharp
using Newtonsoft.Json;

string referenciaJson = JsonConvert.SerializeObject(resultado.Referencia);
SalvarReferencia(idOperacao, referenciaJson);
```

Mais tarde, até em outra execução da aplicação, reconecte à mesma conta e banco e recupere a referência salva:

```csharp
var referencia = JsonConvert.DeserializeObject<ReferenciaPagamento>(referenciaJson);
resultado = await banco.ConsultarPagamentoAsync(referencia);
```

`SalvarReferencia`, `MarcarComoPago` e `ExibirMensagem` representam ações da sua aplicação; não são métodos da biblioteca.

Se a conexão cair antes da resposta do envio, confira se o banco registrou a solicitação antes de enviá-la novamente. Mantenha o mesmo `IdOperacao`, o mesmo `NumeroDocumento` e os mesmos dados do pagamento. A identificação por si só não garante que todos os bancos impeçam pagamentos duplicados.

## Consultar o comprovante ou cancelar um agendamento

Use a mesma referência:

```csharp
var comprovante = await banco.ObterComprovanteAsync(referencia);

// Para cancelar, quando o banco permitir e o pagamento ainda puder ser cancelado:
// resultado = await banco.CancelarPagamentoAsync(referencia);
```

O comprovante pode chegar de formas diferentes:

| Campo | Como usar |
| --- | --- |
| `Conteudo` | Arquivo recebido do banco, pronto para ser salvo |
| `Link` e `ValidadeLink` | Endereço para acessar o arquivo durante o prazo informado |
| `Autenticacao` | Código de autenticação, quando fornecido |
| `Pagamento` | Situação atual e demais informações da consulta |

Use os campos que vierem preenchidos para apresentar o comprovante na sua aplicação. Um pagamento ainda pendente pode não ter comprovante. Algumas integrações devolvem dados de autenticação sem disponibilizar um arquivo.

## Consultar o extrato com mais detalhes

```csharp
var extrato = await banco.ConsultarExtratoDetalhadoAsync(new RequisicaoExtrato
{
    NumeroContaCorrente = numeroDaConta,
    Mes = 10,
    Ano = 2026
});

foreach (var lancamento in extrato.Lancamentos)
{
    var transacao = (MovimentoItemExtratoTransacao)lancamento.Movimento.MovimentoItem;
    Console.WriteLine($"{transacao.Data:d}: {transacao.Descricao} — {transacao.Valor:C}");
}
```

`Movimentos` traz a mesma lista da consulta antiga, incluindo os itens de saldo que o banco fornecer. `Lancamentos` reúne as transações e acrescenta `SaldoAposLancamento` quando essa informação estiver disponível. Um saldo não informado fica nulo.

## Referência dos métodos

| Método | Para que serve |
| --- | --- |
| `EnviarPagamentoAsync(RequisicaoPagamento)` | Enviar e receber o resultado detalhado |
| `ConsultarPagamentoAsync(ReferenciaPagamento)` | Atualizar o acompanhamento |
| `CancelarPagamentoAsync(ReferenciaPagamento)` | Solicitar o cancelamento, quando disponível |
| `ObterComprovanteAsync(ReferenciaPagamento)` | Consultar o comprovante disponível |
| `ConsultarExtratoDetalhadoAsync(RequisicaoExtrato)` | Consultar movimentos e informações adicionais do extrato |

Cada banco mantém suas regras e operações disponíveis. Se uma operação não estiver implementada para a integração escolhida, a biblioteca lança `NotSupportedException`. A consulta prévia explícita de boleto ou chave Pix também depende do suporte do banco.

### Quando precisar investigar uma resposta

`SituacaoOriginal` guarda o estado informado pelo banco. `RetornosBanco` reúne as respostas recebidas durante as etapas da chamada, com seus códigos HTTP. No extrato detalhado, inclui as respostas de todas as páginas consultadas.

Esses campos ajudam a investigar problemas. Para decidir o que mostrar ao usuário, use os estados comuns `Situacao` e `Autorizacao`. As respostas originais podem conter dados bancários e devem ter o mesmo cuidado de acesso e armazenamento dos movimentos. A captura não inclui requisições, headers ou chamadas de autenticação OAuth.

Os erros seguem o tratamento já existente. Em um erro de negócio, `BancoApiException` disponibiliza os códigos e mensagens do banco. Um estado desconhecido permanece disponível para consulta, sem ser interpretado como aprovação ou pagamento realizado.

### Diferenças que continuam existindo entre os bancos

- **Banco do Brasil:** uma solicitação processada ainda precisa ter a situação de cada pagamento consultada. As regras do número de requisição continuam valendo.
- **Santander:** `PENDING_CONFIRMATION` indica processamento; a biblioteca não presume que se trata de uma assinatura pendente.
- **Sicoob:** um boleto aceito com HTTP 202 pode ainda não ter um ID bancário. A referência detalhada guarda os dados necessários para consultá-lo pela identidade do envio. Boleto e convênio podem exigir assinatura.
- **Sicredi:** a solicitação aceita pode depender de aprovação nos canais digitais. Consulte a situação para acompanhar a efetivação.

Se o banco não devolver um identificador e não permitir localizar o pagamento pelo código do envio, confira a solicitação com o banco antes de consultar ou reenviar.

## Documentação relacionada

- [Guia do Sicredi](../src/BancoBr.API/Sicredi/README.md)
- [Guia do Banco do Brasil](../src/BancoBr.API/BancoDoBrasil/README.md)
- [Glossário de pagamentos](../GLOSSARY.md)
- Fontes oficiais: [Pagamentos em Lote BB v1](https://apoio.developers.bb.com.br/apis/23?versaoApi=1), [Multipag Sicredi](https://developers.sicredi.com.br/public/docs/getting-started-multipag) e [aprovação digital Sicredi](https://developers.sicredi.com.br/public/docs/aprova%C3%A7%C3%A3o-digital).

Os testes locais usam respostas simuladas. O uso com uma conta bancária depende das credenciais, do contrato e das permissões liberadas pelo banco.

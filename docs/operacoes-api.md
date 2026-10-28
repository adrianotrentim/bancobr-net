# Operações detalhadas e compatibilidade

As classes e enums de `BancoBr.Common` permanecem iguais aos existentes antes da integração Sicredi. As assinaturas dos métodos antigos de `BancoApi`, a atualização da mesma instância de `Movimento`, seus estados numéricos e o tratamento de erros são preservados. A nova forma de chamada é opcional e está em `BancoBr.API.Operacoes`.

Os quatro bancos implementados — Sicoob, Santander, Banco do Brasil e Sicredi — usam os mesmos métodos novos. Cada um mantém suas modalidades, validações e permissões existentes. Uma operação que não existe no banco continua retornando `NotSupportedException`; o novo contrato não simula recursos bancários ausentes.

## Mesmo pagamento com qualquer conexão

Configure `BancoApi.Conectar` como nos exemplos existentes do banco e utilize:

```csharp
using BancoBr.API.Operacoes;

// Este identificador é gerado e persistido pelo ERP antes do primeiro envio.
Guid idOperacao = identidadePersistidaNoERP;

var requisicao = new RequisicaoPagamento
{
    IdOperacao = idOperacao,
    Movimento = new Movimento
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
    }
};

ResultadoPagamento resultado = await banco.EnviarPagamentoAsync(requisicao);

// Persistir como objeto opaco; não interpretar nem reconstruir seus identificadores.
string referenciaJson = JsonConvert.SerializeObject(resultado.Referencia);
SalvarReferencia(idOperacao, referenciaJson);

// Pode ocorrer em outro processo, após reconstruir a conexão com a mesma conta.
var referencia = JsonConvert.DeserializeObject<ReferenciaPagamento>(referenciaJson);
resultado = await banco.ConsultarPagamentoAsync(referencia);

if (resultado.Autorizacao == SituacaoAutorizacao.AguardandoAutorizacao)
    ExibirMensagem("Autorize o pagamento nos canais do banco.");

if (resultado.Situacao == SituacaoPagamento.Efetivado)
    MarcarComoPago(idOperacao);
```

O exemplo utiliza dados completos e `NumeroDocumento` numérico para atender também às exigências do BB. Nesse banco, o número de requisição deve ser único entre 1 e 9999999 e permanecer igual nas tentativas do mesmo pagamento. `IdOperacao` não substitui essa restrição nem cria uma garantia de deduplicação onde o banco não a oferece. Em caso de resultado incerto, reconcilie a solicitação antes de reenviar, preservando sua identidade e os dados originais.

Nenhuma etapa de autorização ou assinatura é executada automaticamente por estes métodos. O consumidor recebe as informações que o banco forneceu. Não são presumidas a quantidade de assinaturas realizadas nem a quantidade faltante. `NaoInformada` significa ausência de informação de autorização, inclusive quando o pagamento já aparece como efetuado.

## Métodos novos

| Método | Retorno |
| --- | --- |
| `EnviarPagamentoAsync(RequisicaoPagamento)` | Situação de execução, autorização, movimento e referência persistível |
| `ConsultarPagamentoAsync(ReferenciaPagamento)` | Consulta com a mesma conta/banco e atualização da referência |
| `CancelarPagamentoAsync(ReferenciaPagamento)` | Resultado do cancelamento quando suportado |
| `ObterComprovanteAsync(ReferenciaPagamento)` | PDF binário, link temporário ou autenticação/dados, conforme disponíveis |
| `ConsultarExtratoDetalhadoAsync(RequisicaoExtrato)` | Movimentos antigos, saldo após cada lançamento quando fornecido e respostas de todas as páginas |

As chamadas de envio e consulta retornam também `RetornosBanco`, contendo os corpos originais e códigos HTTP das etapas executadas. Esses dados são diagnósticos opcionais; a lógica do consumidor deve usar `Situacao` e `Autorizacao`. As respostas podem conter dados bancários e devem receber o mesmo tratamento de acesso e armazenamento dos movimentos. A captura não inclui as requisições, seus headers ou chamadas OAuth.

Erros continuam sendo propagados como nas chamadas antigas (`BancoApiException` para erros de negócio, com código e mensagem). Estados não reconhecidos permanecem disponíveis no diagnóstico e não são promovidos a autorização ou liquidação.

## Funcionamento das chamadas antigas

```csharp
// Mesmos modelos e assinaturas, inclusive para Sicredi.
await banco.EnviarAgendamentoAsync(movimento, idempotencyKey: idPersistido);
await banco.ConsultarAgendamentoAsync(movimento);
```

Não é necessário preencher propriedades novas no `Common`. O Sicredi usa a identidade fornecida em `idempotencyKey` como `idTransacao`; preserve-a nos reenvios. O estado auxiliar do mesmo objeto é mantido apenas durante sua vida em memória. Para recuperar em outro processo, use a identidade persistida e a referência bancária; a chamada detalhada já devolve essa referência em um contrato serializável.

No Sicoob, um boleto aceito com HTTP 202 pode não ter o identificador bancário ainda. A referência detalhada preserva a identidade de consulta e permite recuperá-lo pelo endpoint de idempotência. Quando outra modalidade não fornece identificador nem consulta por identidade, a referência não pode inventá-lo: é necessário reconciliar com o banco antes de consultar ou reenviar.

Os estados antigos não foram expandidos: uma pendência pode continuar aparecendo como `Agendado` ou `NaoIntegrado`, conforme o adaptador já fazia. A distinção adicional entre processamento e autorização aparece somente no novo resultado, evitando surpreender consumidores antigos com valores novos de enum.

Consulta prévia explícita de boleto/chave permanece sujeita ao suporte do banco. Para o fluxo comum aos quatro bancos, informe os dados completos e utilize o envio: a biblioteca executa as etapas prévias exigidas pelos adaptadores existentes.

## Evidência e normalização

A normalização adicional utiliza os contratos dos clientes existentes e os guias oficiais: [Pagamentos em Lote BB v1](https://apoio.developers.bb.com.br/apis/23?versaoApi=1), [Multipag Sicredi](https://developers.sicredi.com.br/public/docs/getting-started-multipag) e [aprovação digital Sicredi](https://developers.sicredi.com.br/public/docs/aprova%C3%A7%C3%A3o-digital). No BB, o estado da requisição é separado do estado de pagamento: requisição processada não comprova liquidação do lançamento. No Santander, `PENDING_CONFIRMATION` é processamento e não é presumido como assinatura pendente. No Sicoob, HTTP 202 das operações de boleto/convênio indica pendência de assinatura, conforme o fluxo já existente.

A validação local usa HTTP simulado. Homologação autenticada e condições específicas das contas dependem das credenciais, contratos e permissões do banco.

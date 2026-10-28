# Integrações por API

## Contratos e distribuição

- `BancoApi` é a fachada pública: despacha operações para clientes internos pelo tipo de `MovimentoItem`. Preserve `Movimento`/`MovimentoItem` como contrato de domínio, inclusive em consultas de listas.
- A origem vem de `ConfiguracaoAPI.Correntista`. Reutilize essa configuração nos clientes em vez de criar outra fonte de agência e conta.
- Coloque contratos comuns da operação em `Base`; mantenha DTOs e particularidades de protocolo nas pastas do banco. Para uma nova operação, siga a base e o cliente mais próximos e conecte o fluxo à fachada quando aplicável.
- O Santander compartilha transporte e normalização em `Santander/Http/SantanderHttpGateway.cs`. Confira esse gateway antes de duplicar comportamento nos clientes.

## HTTP e pagamentos

- Preserve Newtonsoft.Json e os nomes/formatações de campos definidos pelos DTOs existentes. Verifique URL, método, headers, corpo e interpretação da resposta ao alterar uma chamada.
- Reutilize `Core/OAuth`, `CertificateSource` e os pontos de injeção de `HttpClient` e `IAccessTokenProvider`. Propague `CancellationToken` e acompanhe o padrão de `ConfigureAwait(false)` do transporte.
- Normalize erros de negócio retornados pelo banco em `BancoApiException`/`MensagemErro`, mantendo código e mensagem disponíveis ao consumidor.
- Preserve a identidade do lançamento nas tentativas de pagamento: confira `Core/Models/IdempotencyKey.cs` e seus chamadores antes de alterar a chave. Uma nova tentativa do mesmo lançamento deve manter sua identidade.
- No Santander, confira o uso de `IdWorkspace` e o fluxo explícito de obtenção/criação antes de mudar a descoberta de workspace nas operações.
- Preserve as etapas de consulta, iniciação, confirmação e cancelamento próprias de cada operação. Confirme suporte no cliente e testes; sem evidência, não presuma que bancos diferentes oferecem o mesmo fluxo.

## Evidência e testes

Use os testes em `tests/BancoBr.Tests/Sicoob` e `Santander` como exemplos de HTTP simulado. Para alterações de protocolo, valide a requisição emitida e o mapeamento de sucesso/erro, incluindo reautenticação ou múltiplas etapas quando afetadas. Para mudanças de endpoint, campo ou status bancário, baseie-se em especificação oficial fornecida ou consultada e registre a fonte na entrega; não deduza contratos de outro banco.

Para Sicredi, consulte `Sicredi/README.md` e os guias oficiais vinculados antes de alterar o protocolo. A criação Multipag é assíncrona e não confirma liquidação; preserve `idTransacao` entre tentativas. Homologação usa bases distintas para pagamentos e DDA. Comprovantes são PDF binário, e extrato/saldo usam a conta vinculada à credencial.

# Validação

## Testes locais

`BancoBr.Tests` usa xUnit com `[Fact]` e `[Theory]`. Siga os testes vizinhos e nomeie cenários pelo método, condição e resultado esperado.

- Para API, injete `HttpClient` com handler falso e token fictício. Reutilize `FakeHttpMessageHandler`, `SequencedFakeHttpMessageHandler` e `FakeOAuthTokenProvider` de Sicoob quando adequados, ou o padrão local dos testes Santander.
- Verifique os dados efetivamente enviados e o resultado observável, especialmente identidade do lançamento, conta de origem, situação, identificadores e erros. Use sequências de respostas para fluxos com mais de uma etapa.
- Para CNAB, confira comprimento e conteúdo dos campos afetados. Os testes de `CriaBancoTest` geram e importam o conteúdo em memória; mantenha os cenários independentes da ordem de execução.
- Ao corrigir comportamento, inclua um caso que reproduza a falha quando isso for necessário para demonstrar a correção. Mantenha testes independentes de rede, credenciais reais e horário corrente sempre que possível.

## Console de homologação

`BancoBr.Sicoob.Sandbox` é um executável de integração externa, separado da suíte xUnit. Leia seu `Program.cs` antes de usá-lo. Ele recebe configuração por variáveis de ambiente e pode enviar pagamento quando `SICOOB_CONFIRMAR_PAGAMENTO=true`.

Execute esse console apenas sob solicitação explícita para a integração externa, conforme o `AGENTS.md` da raiz. Não habilite confirmação de pagamento como parte de um build ou teste local. Para conhecer as variáveis disponíveis, consulte o código sem imprimir os valores do ambiente.

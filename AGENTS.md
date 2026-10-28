# Orientações para agentes

## Mapa e leitura

BancoBr.NET é uma biblioteca de integração bancária por API e arquivos CNAB 240. Leia o `README.md` para os fluxos públicos e confirme os detalhes no código e nos testes da operação alterada.

- `src/BancoBr.Common`: domínio compartilhado (`Movimento`, `MovimentoItem`, `Correntista`), enums, atributos e interfaces. É consumido por API e CNAB; avalie ambos ao alterar contratos.
- `src/BancoBr.API`: fachada `BancoApi`, bases por operação, infraestrutura em `Core` e clientes específicos de Sicoob e Santander. Leia também `src/BancoBr.API/AGENTS.md` ao trabalhar nessa área.
- `src/BancoBr.CNAB`: geração e leitura, bases, layouts Febraban e especializações por banco. Leia também `src/BancoBr.CNAB/AGENTS.md` ao trabalhar nessa área.
- `tests`: testes xUnit e console de homologação. Leia `tests/AGENTS.md` antes de alterar ou executar esses projetos.

## Convenções

- As bibliotecas têm alvo `netstandard2.0`; mantenha APIs e dependências compatíveis. Os executáveis de testes têm alvo `net6.0`. Confirme as configurações nos respectivos `.csproj` antes de usar recursos novos.
- Siga o arquivo vizinho: namespaces em bloco, indentação de quatro espaços, tipos e métodos em PascalCase e campos privados geralmente com `_camelCase`. Preserve a terminologia de domínio em português e nomes de protocolo nos DTOs.
- Preserve assinaturas públicas, valores numéricos de enums e contratos de serialização; mudanças neles afetam consumidores e arquivos bancários. Explique qualquer quebra proposta e siga a regra de autorização abaixo.
- Documente contratos públicos e particularidades bancárias em comentários XML em português. Atualize exemplos do README quando o uso público mudar.
- Concentre a alteração na funcionalidade pedida; preserve a formatação dos trechos não alterados.

## Alterações de domínio e modelos

- Preserve os modelos e contratos existentes como primeira opção. Criar, remover ou alterar domínio, modelos, enums ou contratos de entrada/saída exige justificativa concreta e autorização explícita do usuário antes da implementação. A regra inclui Common, Base/Models, DTOs dos bancos e novos modelos públicos em outros pacotes.
- Antes de pedir autorização, apresente o problema real, por que os modelos atuais não o resolvem, a alteração proposta, as alternativas e o impacto nos consumidores, na serialização e em API/CNAB. Aguarde a aprovação da proposta específica.
- Pedidos genéricos de integração, correção ou refatoração não autorizam implicitamente essas mudanças. Uma adição compatível também precisa de autorização; criar outro modo de chamada ou mover os modelos para outro pacote não dispensa a regra.
- Quando a autorização já tiver sido dada para a alteração específica, execute somente o escopo aprovado. Uma solicitação explícita de desfazer uma alteração autoriza sua remoção, sem reintroduzir outros modelos ou ampliar o escopo.

## Validação e entrega

Execute a partir da raiz, conforme a área alterada:

```sh
dotnet restore BancoBr.sln
dotnet build BancoBr.sln --no-restore
dotnet test tests/BancoBr.Tests/BancoBr.Tests.csproj --no-build
```

Para uma iteração focada, use `--filter FullyQualifiedName~NomeDaClasse` no comando de testes. Antes de concluir alterações de código, execute os testes relacionados e amplie para a suíte quando houver impacto compartilhado. Para documentação, confira caminhos, comandos e `git diff --check`.

Informe o comportamento alterado, a validação executada e eventuais bloqueios de ambiente. Diferencie teste aprovado, falha e teste não executado.

## Integrações externas

Use dados fictícios e HTTP simulado para validação local. Certificados, senhas, tokens e dados bancários reais ficam fora dos arquivos versionados e dos logs. Executar o console de homologação ou enviar pagamentos exige solicitação explícita do usuário para essa integração; uma tarefa de código ou documentação não autoriza transações bancárias.

## Manutenção destas instruções

Mantenha regras gerais aqui e detalhes nos arquivos de área. `CLAUDE.md` e `.github/copilot-instructions.md` apenas encaminham a essas fontes. Atualize a orientação afetada quando a arquitetura ou o procedimento de validação mudar.

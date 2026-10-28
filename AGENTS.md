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
- Preserve assinaturas públicas, valores numéricos de enums e contratos de serialização; mudanças neles afetam consumidores e arquivos bancários. Explicite qualquer quebra exigida pela tarefa.
- Documente contratos públicos e particularidades bancárias em comentários XML em português. Atualize exemplos do README quando o uso público mudar.
- Concentre a alteração na funcionalidade pedida; preserve a formatação dos trechos não alterados.

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

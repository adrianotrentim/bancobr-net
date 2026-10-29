# Arquivos CNAB 240

- Comece por `ArquivoCNAB.cs`, `Core/ArquivoCNABExtensions.cs` e pelo `Banco.cs` da instituição afetada para entender montagem, exportação e importação.
- `Base` contém os registros comuns; `Febraban` contém os layouts compartilhados; as pastas dos bancos especializam esses layouts. Prefira uma especialização bancária quando a regra não for comum.
- `CampoCNABAttribute` (em Common) define posição, tamanho, preenchimento e formato. A exportação enumera os atributos por reflexão e concatena os campos em ordem de posição: um intervalo ou uma sobreposição incorreta desloca o registro.
- Preserve registros de 240 caracteres, posições, casas decimais implícitas, formatação de datas, sequenciais e totais de lotes/arquivo. Confira também atributos herdados, sobrescritos e desabilitados ao mudar um segmento.
- Mudanças de domínio, modelos, enums e contratos seguem a regra de autorização do `AGENTS.md` da raiz, mesmo quando propostas para atender um layout bancário.
- Para mudar um layout, identifique banco, segmento e versão do manual que justificam os campos. Não aplique uma particularidade de banco ao layout compartilhado sem evidência.
- Teste os campos afetados nas posições esperadas, além do comprimento das linhas. Cubra importação quando o campo participar do retorno e confira totais quando alterar a montagem dos lotes.
- `tests/BancoBr.Tests/CriaBancoTest.cs` oferece exemplos de remessa/retorno; confira quais bancos e cenários são realmente exercitados antes de usar esse teste como evidência de cobertura.

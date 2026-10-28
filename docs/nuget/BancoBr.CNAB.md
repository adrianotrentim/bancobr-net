# BancoBr.NET — Arquivos CNAB 240

Geração de remessas e leitura de retornos bancários CNAB 240 para pagamentos e transferências no BancoBr.NET.

Oferece a classe ArquivoCNAB para gerar remessas e importar retornos. O suporte a bancos, segmentos e modalidades está detalhado no README do repositório.

## Compatibilidade

Biblioteca com alvo .NET Standard 2.0.

## Instalação

Configure o feed GitHub Packages do proprietário que publicou a versão desejada e execute:

```sh
dotnet add package BancoBr.CNAB --version VERSAO
```

Use uma versão disponível no feed configurado. Versões com sufixo `fork` são distribuições do fork.

## Documentação e licença

Consulte o endereço do projeto nos metadados deste pacote para exemplos, bancos e operações suportados. Credenciais e certificados devem ser fornecidos pela aplicação consumidora e mantidos fora do código versionado.

Distribuído sob a licença MIT, incluída no pacote como `LICENSE`.

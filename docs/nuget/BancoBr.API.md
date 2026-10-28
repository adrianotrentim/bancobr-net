# BancoBr.NET — Integração por API

Integração bancária por API para pagamentos, PIX, TED, DDA e extrato, conforme o suporte de cada instituição.

Oferece a fachada BancoApi e infraestrutura para OAuth2 e certificados mTLS. As operações disponíveis variam por banco. A integração Banco do Brasil está em testes.

## Compatibilidade

Biblioteca com alvo .NET Standard 2.0.

## Instalação

Configure o feed GitHub Packages do proprietário que publicou a versão desejada e execute:

```sh
dotnet add package BancoBr.API --version VERSAO
```

Use uma versão disponível no feed configurado. Versões com sufixo `fork` são distribuições do fork.

## Documentação e licença

Consulte o endereço do projeto nos metadados deste pacote para exemplos, bancos e operações suportados. Credenciais e certificados devem ser fornecidos pela aplicação consumidora e mantidos fora do código versionado.

Distribuído sob a licença MIT, incluída no pacote como `LICENSE`.

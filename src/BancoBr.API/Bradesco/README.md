# Bradesco — em testes

Integração baseada nos manuais do Portal Developers Bradesco: **Pagamento de Boleto de Cobrança v2.0.0**, **Contas de Consumo e Tributos v1.0.1**, **TED v1.0.0**, **Pix Transferências v1.0.3** e **Saldo e Extrato v2.2.2**, com a autenticação mTLS do **Manual do Desenvolvedor v1.0.0**. Usa os mesmos métodos públicos de `BancoApi` dos demais bancos. A homologação bancária ainda está pendente.

| Operação | Situação |
|---|---|
| Boleto (código de barras ou linha digitável): consulta, pagamento, agendamento, cancelamento de agendamento, consulta do pagamento | Implementado |
| Convênio (contas de consumo e tributos por código de barras): consulta, pagamento, agendamento e consulta da situação | Implementado |
| TED: envio e consulta | Implementado |
| Pix por chave ou por agência e conta: envio e consulta | Implementado |
| Extrato de conta corrente (o saldo vem dele: saldo anterior e saldo após o último lançamento) | Implementado |
| Consulta de saldo separada (`ConsultarSaldoAsync`) | Não usada: o endpoint de saldo do Bradesco não foi integrado (`NotSupportedException`) |
| Pix Copia e Cola, DDA, comprovante em PDF, cancelamento de TED, Pix e convênio | Sem API nos manuais (`NotSupportedException`) |

```csharp
var bancoBradesco = BancoApi.Conectar(new ConfiguracaoAPI
{
    ClientId = "seu-client-id",
    ClientSecret = "seu-client-secret",
    CertificadoDigital = certificado, // A1 com razão social + CNPJ no CN; é o mesmo da credencial no portal
    AmbienteHomologacao = true,
    Correntista = new Correntista
    {
        Banco = (int)BancoEnum.BradescoSA,
        NumeroAgencia = 2856, DVAgencia = "0",
        NumeroConta = 50035, DVConta = "6",
        CPF_CNPJ = "12.345.678/0001-90", // titular da conta: define o portador do boleto e a titularidade da TED
        Nome = "EMPRESA LTDA"
    }
});
```

## Endpoints e autenticação

| API | Produção | Homologação |
|---|---|---|
| Token (Boleto e TED) | `https://openapi.bradesco.com.br/auth/server-mtls/v2/token` | `https://openapisandbox.prebanco.com.br/auth/server-mtls/v2/token` |
| Token (Pix) | `https://qrpix.bradesco.com.br/auth/server/oauth/token` | idem sandbox |
| Boleto e TED | `https://openapi.bradesco.com.br/` | `https://openapisandbox.prebanco.com.br/` |
| Pix | `https://qrpix.bradesco.com.br/` | `https://openapisandbox.prebanco.com.br/` |

O token vale 1 hora e é renovado pela biblioteca (inclusive após um HTTP 401). O certificado vai no token e nas chamadas. `tokenEndpointOverride` de `BancoApi.Conectar` troca o endpoint de token dos dois hosts.

## Identificadores controlados pelo chamador

O Bradesco recusa um identificador repetido, então reenviar o mesmo movimento não duplica o pagamento — desde que `NumeroDocumento` seja preservado:

| Operação | `Movimento.NumeroDocumento` | `Movimento.NumeroDocumentoNoBanco` após o envio |
|---|---|---|
| Boleto | `transactionId` numérico, 1 a 9 dígitos | o mesmo `transactionId` (consulta e cancelamento) |
| Convênio | `idTransacao`, 1 a 9 caracteres alfanuméricos | o mesmo `idTransacao` (consulta da situação) |
| Pix | `idtransacao`, 1 a 35 caracteres alfanuméricos | EndToEndId (`e2e`) |
| TED | código identificador (até 25 caracteres, informativo) | `chaveUnicaParaApi` (7 caracteres do documento + data) |

A **TED não deduplica**: se o envio ficar sem resposta, confira o lançamento antes de repetir.

## Particularidades

- **Boleto**: `ConsultarAgendamentoAsync` antes do envio valida o título (Núclea/CIP) e faz a pré-efetivação, preenchendo valor, vencimento, juros/multa (`Acrescimo`), descontos e beneficiário. O número de controle da CIP, exigido em boletos de outros bancos, vai em `IdentificadorConsulta`. A recusa do banco (vencido, já pago...) chega como `BancoApiException` com o código `CBCA…`. Boleto a vencer pago em dia não útil é agendado para o próximo dia útil (`Agendado`); confirme a liquidação com `ConsultarAgendamentoAsync`.
- **Convênio**: duas etapas no mesmo endpoint (`tipoRegistro` 0 valida, 1 paga); a fachada faz a consulta antes de enviar. Aceita o código de 44 dígitos ou a representação numérica de 48. O valor vem de `ValorPagamento` ou, se zero, do próprio código quando ele traz o valor (3º dígito 6 ou 7). Retorno diferente de `000` é recusa, com a descrição do manual em `BancoApiException`, mesmo com HTTP 200. `ConsultarAgendamentoAsync` após o envio procura o `idTransacao` nos débitos efetuados, agendados e não efetuados, nessa ordem. Não atende contas de instituições financeiras (razão 11-02).
- **Extrato**: a conta é a do `Correntista` (agência e conta sem dígito). O primeiro item é o saldo (`SaldoAnterior` da linha "Saldo Anterior" e `SaldoAtual` igual ao saldo após o último lançamento do período), seguido de um item por lançamento (histórico + D-1 + D0; lançamentos futuros ficam de fora). Valores e datas vêm no formato brasileiro e são convertidos. Só `ultimos lançamentos` aparece se a data final for hoje ou futura. O limite é de 5.000 lançamentos por consulta e não há paginação: se o banco indicar mais dados (`codigoRetorno` 01), a consulta falha pedindo um período menor. Lançamentos do banco não trazem identificador único, então `TransactionId` fica vazio.
- **TED**: só efetiva em dia útil entre 06h30 e 17h; não há agendamento. O envio fica `Agendado` (o banco destinatário ainda pode devolver) e a consulta indica `Efetivado` (ACEITA/OK) ou `Rejeitado` (DEVOLVIDA, com o motivo em `DetalheRejeicao`). Contas de favorecido: apenas corrente e poupança.
- **Pix**: a conta pagadora é a do `Correntista`. Com chave Pix, se `Favorecido` tiver CPF/CNPJ, o banco valida que a chave pertence a esse documento e recusa o Pix se não pertencer. `EM_PROCESSAMENTO` aparece como `Agendado`; consulte em até 1 minuto.
- Não há iniciação separada da confirmação no Pix: `ConsultarAgendamentoAsync` antes do envio e o Pix Copia e Cola lançam `NotSupportedException`.

## Pontos a validar na homologação

Os manuais e as collections divergem ou são omissos nestes pontos; confirme no sandbox antes de produção:

- Cabeçalho `Authorization: Bearer <token>` (as collections enviam a variável do token sem o prefixo).
- Consulta de TED por `GET` com query string, como na collection (o manual indica `POST`).
- Conta pagadora do Pix: enviada como número + dígito concatenados (`"500356"`).
- Formato da chave Pix de telefone (`+55DDDNÚMERO`).
- Formato dos corpos de erro de TED e Pix, que os manuais não documentam; o texto bruto é preservado quando não reconhecido.
- Campo `tipoIntermediador` do boleto, enviado como `"D"` (digital).
- Extrato: período sem movimento e erros de bloco com HTTP 200 (`codigoRetorno` diferente de 00 ou 01) são tratados como erro do banco; confirme se um mês vazio devolve `00`.
- Convênio: o que a consulta por `idTransacao` devolve quando o segmento não tem registros (o cliente trata `retorno` diferente de `000` como "não encontrado" e segue para o próximo segmento).
- Token do Pix em produção: o manual do Pix aponta `qrpix.bradesco.com.br/auth/server/oauth/token`, enquanto o manual de token das demais APIs usa `openapi.bradesco.com.br`; foi adotado o do manual do Pix.

# Banco do Brasil — em testes

Integração baseada nas especificações oficiais de [Pagamentos em Lote v1](https://apoio.developers.bb.com.br/apis/23?versaoApi=1) e [Extratos v2](https://apoio.developers.bb.com.br/apis/80?versaoApi=2). Implementa envio e consulta de boleto, convênio com código de barras, TED e Pix por chave, cancelamento de boleto/TED e extrato paginado. A homologação bancária ainda está pendente.

```csharp
var bancoBB = BancoApi.Conectar(new ConfiguracaoAPI
{
    ClientId = "seu-client-id",
    ClientSecret = "seu-client-secret",
    ChaveAplicacao = "sua-developer-application-key",
    CertificadoDigital = certificado,
    AmbienteHomologacao = true,
    // NumeroContratoPagamento = 123456, // quando previsto no contrato com o BB
    Correntista = new Correntista
    {
        Banco = (int)BancoEnum.BancoDoBrasil,
        NumeroAgencia = 1234,
        NumeroConta = 56789,
        DVConta = "X"
    }
});

var pagamentoBB = new Movimento
{
    NumeroDocumento = "1234567", // numeroRequisicao único, de 1 a 9999999, controlado pelo cliente
    DataPagamento = DateTime.Today,
    ValorPagamento = 150.00m,
    MovimentoItem = new MovimentoItemTransferenciaPIX
    {
        TipoChavePIX = FormaIniciacaoEnum.PIX_Email,
        ChavePIX = "fornecedor@example.com"
    }
};
await bancoBB.EnviarAgendamentoAsync(pagamentoBB);
await bancoBB.ConsultarAgendamentoAsync(pagamentoBB);
var extratoBB = await bancoBB.ConsultarExtratoAsync(56789, mes: 10, ano: 2026);
```

A aplicação precisa estar habilitada para os produtos e escopos usados no Portal Developers BB. A autenticação usa OAuth2 client credentials com Basic e certificado mTLS nas chamadas das APIs; também é possível fornecer um `IAccessTokenProvider` em `Conectar`.

Cada envio utiliza uma requisição com um lançamento. Preserve `NumeroDocumento` nas tentativas do mesmo pagamento e use outro número para um novo pagamento; o argumento `idempotencyKey` da fachada não substitui esse número no BB. Informe valor e data de pagamento; para boletos, também beneficiário (CPF/CNPJ), valor nominal e código de barras de 44 dígitos. Convênios também exigem código de barras de 44 dígitos. O BB não oferece consulta prévia nesses clientes: envie os dados completos antes de consultar o pagamento pelo identificador retornado.

O aceite da requisição fica como `NaoIntegrado` até a consulta confirmar a situação bancária. A liberação/autorização exigida pelo contrato deve ser feita pelos canais do BB; esta biblioteca não libera lotes automaticamente. DDA, Pix QR Code, consulta prévia de chave Pix e link PDF de comprovante não estão implementados para o BB.

O extrato usa agência e conta de `ConfiguracaoAPI.Correntista`, percorre todas as páginas e retorna lançamentos contabilizados e a posição de saldo atual quando informada. Lançamentos futuros ou em processamento não são incluídos. `numeroContaCorrente` e `agruparCnab` não alteram a consulta do BB.


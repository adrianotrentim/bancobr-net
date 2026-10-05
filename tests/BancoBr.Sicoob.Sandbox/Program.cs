using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Core.Errors;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using BancoBr.API;

// Console standalone para validar a integração Sicoob (PagamentoBoletoClient) contra um
// ambiente real (sandbox/homologação), sem depender de um ERP. Nenhuma credencial é lida
// de arquivo de configuração commitado: tudo vem de variáveis de ambiente.

string? clientId = Environment.GetEnvironmentVariable("SICOOB_CLIENT_ID");
string? accessToken = Environment.GetEnvironmentVariable("SICOOB_ACCESS_TOKEN");
string? tokenEndpoint = Environment.GetEnvironmentVariable("SICOOB_TOKEN_ENDPOINT");
string? certPfxPath = Environment.GetEnvironmentVariable("SICOOB_CERT_PFX_PATH");
string? certPassword = Environment.GetEnvironmentVariable("SICOOB_CERT_PASSWORD");
string? numeroAgenciaText = Environment.GetEnvironmentVariable("SICOOB_NUMERO_COOPERATIVA");
string? numeroContaText = Environment.GetEnvironmentVariable("SICOOB_NUMERO_CONTA");
string? codigoBarras = Environment.GetEnvironmentVariable("SICOOB_CODIGO_BARRAS");
string? idLancamentoText = Environment.GetEnvironmentVariable("SICOOB_ID_LANCAMENTO");
Guid idLancamento = string.IsNullOrWhiteSpace(idLancamentoText) ? Guid.NewGuid() : Guid.Parse(idLancamentoText);
bool confirmarPagamento = Environment.GetEnvironmentVariable("SICOOB_CONFIRMAR_PAGAMENTO") == "true";

// O Sicoob expõe, no portal de sandbox, um "Access token (Bearer)" já emitido para teste
// rápido — sem precisar implementar o fluxo client_credentials completo. SICOOB_ACCESS_TOKEN
// usa esse token diretamente; sem ele, cai no fluxo OAuth2 real (SicoobApiOptions já traz o
// endpoint de token padrão — https://auth.sicoob.com.br/... — só sobrescrito se SICOOB_TOKEN_ENDPOINT
// for definido, para algum ambiente fora do padrão).
bool usarTokenFixo = !string.IsNullOrWhiteSpace(accessToken);

var faltando = new List<string>();
if (string.IsNullOrWhiteSpace(clientId)) faltando.Add("SICOOB_CLIENT_ID");
if (string.IsNullOrWhiteSpace(certPfxPath)) faltando.Add("SICOOB_CERT_PFX_PATH");
if (string.IsNullOrWhiteSpace(certPassword)) faltando.Add("SICOOB_CERT_PASSWORD");
if (string.IsNullOrWhiteSpace(numeroAgenciaText)) faltando.Add("SICOOB_NUMERO_COOPERATIVA");
if (string.IsNullOrWhiteSpace(numeroContaText)) faltando.Add("SICOOB_NUMERO_CONTA");
if (string.IsNullOrWhiteSpace(codigoBarras)) faltando.Add("SICOOB_CODIGO_BARRAS");

if (faltando.Count > 0)
{
    Console.WriteLine("Variáveis de ambiente obrigatórias ausentes:");
    foreach (var nome in faltando)
    {
        Console.WriteLine($"  - {nome}");
    }
    Console.WriteLine();
    Console.WriteLine("Opcional: SICOOB_ACCESS_TOKEN para usar um token Bearer já emitido pelo portal de sandbox, pulando o fluxo OAuth2.");
    Console.WriteLine("Opcional: SICOOB_TOKEN_ENDPOINT para sobrescrever o endpoint de token padrão do Sicoob.");
    Console.WriteLine("Opcional: SICOOB_CONFIRMAR_PAGAMENTO=true para também testar PagarBoletoAsync (cuidado: dispara um pagamento real, mesmo em sandbox).");
    Console.WriteLine("Opcional: SICOOB_ID_LANCAMENTO (UUID) para fixar o lançamento usado na idempotency key (default: UUID gerado a cada execução).");
    return 1;
}

var numeroAgencia = int.Parse(numeroAgenciaText!);
var numeroConta = long.Parse(numeroContaText!);

var certificateSource = CertificateSource.FromPfxFile(certPfxPath!, certPassword!);
var tokenEndpointOverride = string.IsNullOrWhiteSpace(tokenEndpoint) ? null : new Uri(tokenEndpoint);

var banco = usarTokenFixo
    ? BancoApi.Conectar(BancoEnum.Sicoob, clientId!, certificateSource, new StaticAccessTokenProvider(accessToken!))
    : BancoApi.Conectar(BancoEnum.Sicoob, clientId!, clientSecret: null, certificateSource, tokenEndpointOverride);

// A conta pagadora é passada como Correntista, do mesmo jeito que ArquivoCNAB recebe a
// empresa separada da lista de movimentos.
var origem = new Correntista
{
    NumeroAgencia = numeroAgencia,
    NumeroConta = (int)numeroConta,
    TipoPessoa = TipoInscricaoCPFCNPJEnum.CNPJ,
};

// O contrato público de BancoBr.API é o mesmo Movimento/MovimentoItem usado pelo CNAB.
var movimento = new Movimento
{
    TipoLancamento = TipoLancamentoEnum.PagamentoTituloOutroBanco,
    MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra { CodigoBarras = codigoBarras! },
};

try
{
    Console.WriteLine($"Consultando boleto {codigoBarras} na conta {numeroConta}...");
    await banco.ConsultarAgendamentoAsync(movimento, origem);

    var item = (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem;

    if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Cancelado && item.IdentificadorConsulta == null)
    {
        Console.WriteLine($"Boleto não disponível para pagamento: {movimento.DetalheRejeicaoBancoBr}");
        return 0;
    }

    Console.WriteLine($"Beneficiário: {movimento.Favorecido?.Nome} ({movimento.Favorecido?.CPF_CNPJ})");
    Console.WriteLine($"Valor do boleto: {item.ValorCodigoBarra:C}");
    Console.WriteLine($"Valor de pagamento: {movimento.ValorPagamento:C}");
    Console.WriteLine($"Vencimento: {item.DataVencimento:yyyy-MM-dd}");
    Console.WriteLine($"IdentificadorConsulta: {item.IdentificadorConsulta}");
    Console.WriteLine($"Situação: {movimento.SituacaoBancoBr} ({movimento.DetalheRejeicaoBancoBr})");

    if (!confirmarPagamento)
    {
        Console.WriteLine();
        Console.WriteLine("EnviarAgendamentoAsync não foi chamado (defina SICOOB_CONFIRMAR_PAGAMENTO=true para testar o pagamento).");
        return 0;
    }

    if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Cancelado)
    {
        Console.WriteLine("Pagamento bloqueado pelo Sicoob para este boleto; abortando antes de chamar EnviarAgendamentoAsync.");
        return 1;
    }

    Console.WriteLine();
    Console.WriteLine("SICOOB_CONFIRMAR_PAGAMENTO=true: chamando EnviarAgendamentoAsync...");

    // O portador enviado ao banco vem do Correntista; aqui reaproveitamos o que o próprio
    // boleto devolveu como pagador, para o sandbox não exigir mais variáveis de ambiente.
    origem.Nome = item.PagadorConfirmadoNome;
    origem.CPF_CNPJ = item.PagadorConfirmadoDocumento;

    // ConsultarAgendamentoAsync já deixou IdentificadorConsulta preenchido no movimento, então
    // EnviarAgendamentoAsync paga direto com esses dados (sem consultar de novo) — só precisa do
    // idLancamento como idempotencyKey; a chave composta do Sicoob é montada por baixo.
    await banco.EnviarAgendamentoAsync(movimento, origem, idLancamento.ToString());

    Console.WriteLine($"Pagamento processado. Situação={movimento.SituacaoBancoBr}, IdPagamento={movimento.NumeroDocumentoNoBanco}, Detalhe={movimento.DetalheRejeicaoBancoBr}");

    return 0;
}
catch (BancoApiException ex)
{
    Console.WriteLine($"Erro de negócio do Sicoob (HTTP {ex.HttpStatusCode}):");
    foreach (var mensagem in ex.Mensagens)
    {
        Console.WriteLine($"  [{mensagem.Codigo}] {mensagem.Mensagem}");
    }
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine("Falha de transporte/autenticação (cert/mTLS/OAuth/rede), não um erro de negócio do Sicoob:");
    Console.WriteLine(ex);
    return 1;
}

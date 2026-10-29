using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

// Somente homologação, consultas de extrato/saldo e de um pagamento já existente.
// Não imprime respostas bancárias, tokens, documentos ou exceções que possam conter segredos.
var obrigatorias = new[] { "SICREDI_CLIENT_ID", "SICREDI_CLIENT_SECRET", "SICREDI_CERT_PFX_PATH", "SICREDI_COOPERATIVA", "SICREDI_CONTA", "SICREDI_DV_CONTA", "SICREDI_DOCUMENTO" };
string? Ler(string nome) => Environment.GetEnvironmentVariable(nome);
var ausentes = obrigatorias.Where(n => string.IsNullOrWhiteSpace(Ler(n))).ToArray();
if (ausentes.Length > 0)
{
    Console.WriteLine("Comunicação não executada. Configure: " + string.Join(", ", ausentes));
    return 2;
}
try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
    var banco = BancoApi.Conectar(new ConfiguracaoAPI
    {
        AmbienteHomologacao = true, ClientId = Ler("SICREDI_CLIENT_ID"), ClientSecret = Ler("SICREDI_CLIENT_SECRET"),
        CertificadoDigital = CertificateSource.FromPfxFile(Ler("SICREDI_CERT_PFX_PATH")!, Ler("SICREDI_CERT_PASSWORD") ?? ""),
        Correntista = new Correntista { Banco = (int)BancoEnum.Sicredi, NumeroAgencia = int.Parse(Ler("SICREDI_COOPERATIVA")!),
            NumeroConta = int.Parse(Ler("SICREDI_CONTA")!), DVConta = Ler("SICREDI_DV_CONTA"), CPF_CNPJ = Ler("SICREDI_DOCUMENTO") }
    });
    await banco.ConsultarSaldoAsync(timeout.Token);
    Console.WriteLine("Saldo: comunicação aprovada.");
    var data = DateTime.ParseExact(Ler("SICREDI_DATA_EXTRATO") ?? "2026-06-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    await banco.ConsultarExtratoAsync(0, data.Month, data.Year, cancellationToken: timeout.Token);
    Console.WriteLine("Extrato: comunicação aprovada.");
    if (!string.IsNullOrWhiteSpace(Ler("SICREDI_ID_TRANSACAO")))
    {
        await banco.ConsultarAgendamentoAsync(new Movimento { NumeroDocumentoNoBanco = Ler("SICREDI_ID_TRANSACAO"), MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra() }, cancellationToken: timeout.Token);
        Console.WriteLine("Multipag boleto: comunicação aprovada.");
    }
    else Console.WriteLine("Multipag não executado: configure SICREDI_ID_TRANSACAO com uma transação de teste fornecida pelo Sicredi.");
    return 0;
}
catch (BancoBr.API.Core.Errors.BancoApiException e)
{
    Console.WriteLine($"Comunicação rejeitada pelo banco (HTTP {e.HttpStatusCode}).");
    return 1;
}
catch (Exception e)
{
    Console.WriteLine("Falha de configuração ou comunicação: " + e.GetType().Name);
    return 1;
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.BancoDoBrasil.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.BancoDoBrasil.Pagamentos
{
    /// <summary>Adapta o contrato de movimentos para a API Pagamentos em Lote v1 do Banco do Brasil.</summary>
    internal class PagamentoBoletoClient : PagamentoBoletoApiBase
    {
        private readonly PagamentosClient _pagamentos;
        internal PagamentoBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string chaveAplicacao, Correntista origem, int? numeroContratoPagamento = null, Uri baseUrl = null) : base(httpClient, origem)
        {
            _pagamentos = new PagamentosClient(new BancoDoBrasilHttpGateway(httpClient, tokenProvider, chaveAplicacao), origem, numeroContratoPagamento, baseUrl);
        }
        internal override Task<Movimento> PagarBoletoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var item = movimento.MovimentoItem as MovimentoItemPagamentoTituloCodigoBarra ?? throw new ArgumentException("Informe boleto.");
            if (movimento.Favorecido == null || string.IsNullOrWhiteSpace(movimento.Favorecido.CPF_CNPJ)) throw new ArgumentException("Informe CPF/CNPJ do beneficiário do boleto.");
            return _pagamentos.EnviarAsync(movimento, "lotes-boletos", new JObject {
                ["numeroCodigoBarras"] = PagamentosClient.Codigo(item.CodigoBarras), ["dataPagamento"] = PagamentosClient.Data(movimento.DataPagamento),
                ["valorPagamento"] = movimento.ValorPagamento, ["codigoTipoBeneficiario"] = movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ ? 2 : 1,
                ["documentoBeneficiario"] = PagamentosClient.Digitos(movimento.Favorecido.CPF_CNPJ), ["codigoSeuDocumento"] = movimento.NumeroDocumento,
                ["valorNominal"] = item.ValorCodigoBarra, ["valorDesconto"] = item.Desconto, ["valorMoraMulta"] = item.Acrescimo
            }, cancellationToken);
        }
        internal override Task<Movimento> PagarBoletoComConsultaAsync(Movimento movimento, Guid idLancamento, CancellationToken cancellationToken = default) => PagarBoletoAsync(movimento, null, cancellationToken);
        internal override Task<Movimento> ConsultarComprovantePorIdAsync(Movimento movimento, CancellationToken cancellationToken = default) => _pagamentos.ConsultarAsync(movimento, "boletos", cancellationToken);
        internal override Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, CancellationToken cancellationToken = default) => _pagamentos.CancelarAsync(movimento, cancellationToken);
        internal override Task<Movimento> ConsultarBoletoAsync(Movimento movimento, CancellationToken cancellationToken = default) => ConsultarComprovantePorIdAsync(movimento, cancellationToken);
        internal override Task<Movimento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override Task<IReadOnlyList<Movimento>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override async Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<(Movimento Movimento, Guid IdLancamento)> itens, CancellationToken cancellationToken = default)
        {
            var resultados = new List<PagamentoBoletoLoteResultadoItem>();
            foreach (var item in itens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { await PagarBoletoAsync(item.Movimento, null, cancellationToken).ConfigureAwait(false); resultados.Add(PagamentoBoletoLoteResultadoItem.ComSucesso(item.Movimento, item.IdLancamento)); }
                catch (Exception ex) when (!(ex is OperationCanceledException)) { resultados.Add(PagamentoBoletoLoteResultadoItem.ComFalha(item.Movimento, item.IdLancamento, ex)); }
            }
            return resultados;
        }
    }
}

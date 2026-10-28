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
    internal class PagamentoConvenioClient : PagamentoConvenioApiBase
    {
        private readonly PagamentosClient _pagamentos;
        internal override bool ExigeConsultaPrevia => false;
        internal PagamentoConvenioClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string chaveAplicacao, Correntista origem, Uri baseUrl = null) : base(httpClient, origem)
        {
            _pagamentos = new PagamentosClient(new BancoDoBrasilHttpGateway(httpClient, tokenProvider, chaveAplicacao), origem, baseUrl);
        }
        internal override Task<Movimento> PagarConvenioAsync(Movimento movimento, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra ?? throw new ArgumentException("Informe convênio.");
            return _pagamentos.EnviarAsync(movimento, "lotes-guias-codigo-barras", new JObject { ["codigoBarras"] = PagamentosClient.Codigo(item.CodigoBarra),
                ["dataPagamento"] = PagamentosClient.Data(movimento.DataPagamento), ["valorPagamento"] = movimento.ValorPagamento, ["codigoSeuDocumento"] = movimento.NumeroDocumento }, cancellationToken);
        }
        internal override Task<Movimento> ConsultarComprovantePorNsuAsync(Movimento movimento, CancellationToken cancellationToken = default) => _pagamentos.ConsultarAsync(movimento, "guias-codigo-barras", cancellationToken);
        internal override Task<Movimento> ConsultarCodigoBarrasAsync(Movimento movimento, bool? recebimentoViaCaixa = null, int unidade = 0, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override Task<IReadOnlyList<Movimento>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
    }
}

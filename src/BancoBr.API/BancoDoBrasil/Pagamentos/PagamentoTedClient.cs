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
    internal class PagamentoTedClient : PagamentoTedApiBase
    {
        private readonly PagamentosClient _pagamentos;
        internal PagamentoTedClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string chaveAplicacao, Correntista origem, int? numeroContratoPagamento = null, Uri baseUrl = null) : base(httpClient, origem)
        {
            _pagamentos = new PagamentosClient(new BancoDoBrasilHttpGateway(httpClient, tokenProvider, chaveAplicacao), origem, numeroContratoPagamento, baseUrl);
        }
        internal override Task<Movimento> PagarTedAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var item = movimento.MovimentoItem as MovimentoItemTransferenciaTED ?? throw new ArgumentException("Informe TED.");
            var beneficiario = movimento.Favorecido ?? throw new ArgumentException("Informe beneficiário.");
            var lancamento = new JObject { ["numeroCOMPE"] = item.Banco, ["agenciaCredito"] = item.NumeroAgencia,
                ["contaCorrenteCredito"] = item.NumeroConta, ["digitoVerificadorContaCorrente"] = item.DVConta,
                ["dataTransferencia"] = PagamentosClient.Data(movimento.DataPagamento), ["valorTransferencia"] = movimento.ValorPagamento,
                ["codigoFinalidadeTED"] = (int)item.CodigoFinalidadeTED, ["descricaoTransferencia"] = item.Historico };
            if (beneficiario.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ) lancamento["cnpjBeneficiario"] = PagamentosClient.Digitos(beneficiario.CPF_CNPJ);
            else lancamento["cpfBeneficiario"] = long.Parse(PagamentosClient.Digitos(beneficiario.CPF_CNPJ));
            return _pagamentos.EnviarAsync(movimento, "lotes-transferencias", lancamento, cancellationToken);
        }
        internal override Task<Movimento> ConsultarTedAsync(Movimento movimento, CancellationToken cancellationToken = default) => _pagamentos.ConsultarAsync(movimento, "transferencias", cancellationToken);
        internal override Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default) => _pagamentos.CancelarAsync(movimento, cancellationToken);
    }
}

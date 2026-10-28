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
    internal class PagamentoPixClient : PagamentoPixApiBase
    {
        private readonly PagamentosClient _pagamentos;
        internal PagamentoPixClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string chaveAplicacao, Correntista origem, Uri baseUrl = null) : base(httpClient, origem)
        {
            _pagamentos = new PagamentosClient(new BancoDoBrasilHttpGateway(httpClient, tokenProvider, chaveAplicacao), origem, baseUrl);
        }
        internal override Task<Movimento> PagarComIniciacaoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = movimento.MovimentoItem as MovimentoItemTransferenciaPIX ?? throw new ArgumentException("Informe Pix por chave.");
            var lancamento = new JObject { ["data"] = PagamentosClient.Data(movimento.DataPagamento), ["valor"] = movimento.ValorPagamento, ["formaIdentificacao"] = (int)item.TipoChavePIX };
            switch (item.TipoChavePIX)
            {
                case FormaIniciacaoEnum.PIX_Email: lancamento["email"] = item.ChavePIX; break;
                case FormaIniciacaoEnum.PIX_Aleatorio: lancamento["identificacaoAleatoria"] = item.ChavePIX; break;
                case FormaIniciacaoEnum.PIX_CPF_CNPJ:
                    var doc = PagamentosClient.Digitos(item.ChavePIX);
                    if (doc.Length == 14) lancamento["cnpj"] = doc; else if (doc.Length == 11) lancamento["cpf"] = long.Parse(doc); else throw new ArgumentException("Chave CPF/CNPJ inválida.");
                    break;
                case FormaIniciacaoEnum.PIX_Telefone:
                    var telefone = PagamentosClient.Digitos(item.ChavePIX);
                    if (telefone.StartsWith("55") && telefone.Length == 13) telefone = telefone.Substring(2);
                    if (telefone.Length != 11) throw new ArgumentException("Informe telefone brasileiro com DDD.");
                    lancamento["dddTelefone"] = int.Parse(telefone.Substring(0, 2)); lancamento["telefone"] = long.Parse(telefone.Substring(2)); break;
                default: throw new NotSupportedException("Forma de identificação Pix não suportada.");
            }
            return _pagamentos.EnviarAsync(movimento, "lotes-transferencias-pix", lancamento, cancellationToken);
        }
        internal override Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default) => _pagamentos.ConsultarAsync(movimento, "pix", cancellationToken);
        internal override Task<Movimento> IniciarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
        internal override Task<Movimento> PagarViaQrCodeAsync(Movimento movimento, CancellationToken cancellationToken = default) { throw new NotSupportedException("Operação não disponível nesta integração BB de Pagamentos em Lote."); }
    }
}

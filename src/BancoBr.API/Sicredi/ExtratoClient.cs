using System;
using BancoBr.API.Core.Operacoes;
using System.Globalization;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Sicredi
{
    internal sealed class ExtratoClient : ContaCorrenteApiBase, ISaldoApi
    {
        private readonly SicrediHttpGateway _gateway;
        internal ExtratoClient(SicrediHttpGateway gateway) : base(null, gateway.Origem) { _gateway = gateway; }
        internal override async Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default)
        {
            if (agruparCnab) throw new NotSupportedException("Sicredi não oferece agrupamento CNAB.");
            var inicio = new DateTime(ano, mes, diaInicial ?? 1);
            var fim = new DateTime(ano, mes, diaFinal ?? DateTime.DaysInMonth(ano, mes));
            if (inicio > fim) throw new ArgumentException("Período invertido.");
            var resultado = new List<Movimento>();
            var saldo = new MovimentoItemExtratoSaldo();
            resultado.Add(new Movimento { MovimentoItem = saldo });
            for (var page = 0; ; page++)
            {
                var r = await _gateway.JsonAsync(HttpMethod.Get, _gateway.Raiz + "contacorrente/v1/extratos?dataInicio=" + inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "&dataFim=" + fim.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "&page=" + page + "&size=100", null, cancellationToken).ConfigureAwait(false);
                if (r.Value<int?>("pageNumber") != page) throw new JsonSerializationException("Página do extrato divergente da solicitada.");
                if (page == 0) saldo.SaldoAtual = saldo.SaldoAnterior = r.Value<decimal>("saldoAnterior");
                foreach (var item in (JArray)r["dtlMovimentos"] ?? throw new JsonSerializationException("Extrato sem dtlMovimentos."))
                {
                    var valor = item.Value<decimal>("valor");
                    saldo.SaldoAtual = item.Value<decimal>("saldo");
                    var transacao = new MovimentoItemExtratoTransacao
                    {
                        TransactionId = (string)item["idMovimento"], Data = item.Value<DateTime>("data"),
                        Descricao = (string)item["descricao"], DescricaoInformacaoComplementar = (string)item["complemento"],
                        NumeroDocumento = (string)item["documento"], Valor = Math.Abs(valor),
                        Tipo = valor < 0 ? BancoBrTipoCreditoDebitoEnum.Debito : BancoBrTipoCreditoDebitoEnum.Credito
                    };
                    MetadadosOperacao.De(transacao).SaldoAposLancamento = item.Value<decimal?>("saldo");
                    resultado.Add(new Movimento { MovimentoItem = transacao });
                }
                if (page + 1 >= r.Value<int>("totalPages")) break;
            }
            return resultado;
        }

        public async Task<Movimento> ConsultarSaldoAsync(CancellationToken ct)
        {
            var r = await _gateway.JsonAsync(HttpMethod.Get, _gateway.Raiz + "contacorrente/v1/saldos", null, ct).ConfigureAwait(false);
            return new Movimento { MovimentoItem = new MovimentoItemExtratoSaldo
            {
                SaldoAtual = r.Value<decimal>("saldoAtual"), SaldoBloqueado = r.Value<decimal>("saldoBloqueadoParaCheques") + r.Value<decimal>("saldoRetido"),
                SaldoBloqueioJudicial = r.Value<decimal>("saldoBloqueioJudicial"), SaldoLimite = r.Value<decimal>("limiteChequeEspecialDisponivel")
            }};
        }
    }
}

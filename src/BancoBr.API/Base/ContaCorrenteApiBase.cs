using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de conta corrente (saldo/extrato), independente do
    /// banco — mesmo papel que <see cref="PagamentoBoletoApiBase"/> tem para pagamento de
    /// boletos.
    /// </summary>
    internal abstract class ContaCorrenteApiBase
    {
        protected ContaCorrenteApiBase(HttpClient httpClient, Correntista origem)
        {
            HttpClient = httpClient;
            Origem = origem;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>Conta pagadora/titular informada em <c>ConfiguracaoAPI.Correntista</c>.</summary>
        protected Correntista Origem { get; }

        /// <summary>
        /// Consulta o extrato da conta no mês/ano informado. No Sicoob, o primeiro
        /// <see cref="Movimento"/> da lista traz o saldo do período
        /// (<see cref="MovimentoItemExtratoSaldo"/> — o banco não dá saldo por lançamento, só do
        /// período inteiro); os demais são um por lançamento, com
        /// <see cref="MovimentoItemExtratoTransacao"/>. O Santander não devolve saldo: só os
        /// lançamentos. <paramref name="diaInicial"/> e <paramref name="diaFinal"/> recortam o
        /// período dentro daquele mês (nulos = mês inteiro). <paramref name="agruparCnab"/> agrupa
        /// o movimento proveniente de CNAB (só Sicoob). <c>Origem</c> identifica a
        /// conta por agência/conta/DV e é obrigatório no Santander (no Sicoob vale
        /// <paramref name="numeroContaCorrente"/>).
        /// </summary>
        internal abstract Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default);
    }
}

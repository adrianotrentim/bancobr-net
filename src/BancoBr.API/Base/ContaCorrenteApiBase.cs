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
        protected ContaCorrenteApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Consulta o extrato da conta no mês/ano informado. O primeiro <see cref="Movimento"/>
        /// da lista traz o saldo do período (<see cref="MovimentoItemExtratoSaldo"/> — o
        /// Sicoob não dá saldo por lançamento, só do período inteiro); os demais são um por
        /// lançamento, com <see cref="MovimentoItemExtratoTransacao"/>.
        /// <paramref name="diaInicial"/> e <paramref name="diaFinal"/> recortam o período dentro
        /// daquele mês (nulos = mês inteiro). <paramref name="agruparCnab"/> agrupa o movimento
        /// proveniente de CNAB.
        /// </summary>
        internal abstract Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default);
    }
}

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de envio de TED (SPB Transferências), independente do
    /// banco — mesma ideia de <see cref="PagamentoBoletoApiBase"/>/<see cref="PagamentoConvenioApiBase"/>:
    /// cada banco herda e implementa os métodos abaixo de acordo com sua própria API.
    ///
    /// O contrato público é o mesmo <see cref="Movimento"/>/<see cref="MovimentoItem"/> usado
    /// pelo BancoBr.CNAB (a operação de TED espera um
    /// <see cref="MovimentoItemTransferenciaTED"/> como <see cref="Movimento.MovimentoItem"/>).
    /// Diferente de Boleto/Convênio, a API SPB não tem endpoint de consulta prévia por
    /// conta/beneficiário — o envio é direto.
    /// </summary>
    internal abstract class PagamentoTedApiBase
    {
        protected PagamentoTedApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Envia a TED. O identificador devolvido pelo banco (numeroControleIF) fica em
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/>, usado depois por
        /// <see cref="ConsultarTedAsync"/>; o idAgendamento (usado para cancelar) fica em
        /// <see cref="MovimentoItemTransferenciaTED.IdAgendamento"/>.
        /// </summary>
        internal abstract Task<Movimento> PagarTedAsync(Movimento movimento, Correntista origem, string idempotencyKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta a TED identificada por <see cref="Movimento.NumeroDocumentoNoBanco"/>
        /// (numeroControleIF, devolvido por <see cref="PagarTedAsync"/>).
        /// </summary>
        internal abstract Task<Movimento> ConsultarTedAsync(Movimento movimento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela o agendamento de uma TED ainda não liquidada, identificada por
        /// <see cref="MovimentoItemTransferenciaTED.IdAgendamento"/>.
        /// </summary>
        internal abstract Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default);
    }
}

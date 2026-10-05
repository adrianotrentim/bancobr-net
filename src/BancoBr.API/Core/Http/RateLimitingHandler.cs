using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BancoBr.API.Core.Http
{
    /// <summary>
    /// Limita a taxa de requisições por segundo. Cada API do Sicoob documenta seu próprio
    /// limite (ex.: Pagamentos de Boletos = 2 req/s); por isso a taxa é configurável por instância.
    /// Não implementa retry/backoff pois o Sicoob não documenta esse comportamento.
    /// </summary>
    internal class RateLimitingHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly TimeSpan _interval;

        public RateLimitingHandler(int requestsPerSecond)
        {
            if (requestsPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requestsPerSecond), "Deve ser maior que zero.");
            }

            _semaphore = new SemaphoreSlim(requestsPerSecond, requestsPerSecond);
            _interval = TimeSpan.FromSeconds(1);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _ = ReleaseAfterIntervalAsync();
            }
        }

        private async Task ReleaseAfterIntervalAsync()
        {
            await Task.Delay(_interval).ConfigureAwait(false);
            _semaphore.Release();
        }
    }
}

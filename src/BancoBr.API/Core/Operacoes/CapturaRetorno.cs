using System;
using System.Collections.Generic;
using System.Threading;
using BancoBr.API.Operacoes;

namespace BancoBr.API.Core.Operacoes
{
    /// <summary>Captura somente respostas de recursos bancários da chamada detalhada atual. Não captura OAuth, headers ou requisições.</summary>
    internal sealed class CapturaRetorno : IDisposable
    {
        private static readonly AsyncLocal<CapturaRetorno> Atual = new AsyncLocal<CapturaRetorno>();
        private readonly CapturaRetorno _anterior;
        internal List<RetornoBancario> Retornos { get; } = new List<RetornoBancario>();
        internal CapturaRetorno()
        {
            _anterior = Atual.Value;
            Atual.Value = this;
        }
        internal static void Registrar(string corpo, int httpStatusCode)
        {
            Atual.Value?.Retornos.Add(new RetornoBancario { Corpo = corpo, HttpStatusCode = httpStatusCode });
        }
        public void Dispose() { Atual.Value = _anterior; }
    }
}

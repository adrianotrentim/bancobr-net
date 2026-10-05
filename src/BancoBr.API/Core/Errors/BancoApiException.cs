using System;
using System.Collections.Generic;

namespace BancoBr.API.Core.Errors
{
    /// <summary>
    /// Erro de negócio retornado pela API do banco (HTTP com corpo trazendo mensagens de erro).
    /// Único tipo de exceção da BancoBr.API — não uma classe por banco: o que muda de banco pra
    /// banco é só o formato do corpo de erro na wire, e cada client já normaliza isso para
    /// <see cref="Mensagens"/> antes de lançar.
    /// </summary>
    public class BancoApiException : Exception
    {
        public int? HttpStatusCode { get; }

        /// <summary>
        /// Mensagens de erro devolvidas pelo banco. Os códigos são documentados por cada banco;
        /// não são mapeados exaustivamente em tipos próprios nesta versão — o chamador pode
        /// inspecionar Mensagens[].Codigo quando precisar.
        /// </summary>
        public IReadOnlyList<MensagemErro> Mensagens { get; }

        public BancoApiException(int? httpStatusCode, IReadOnlyList<MensagemErro> mensagens)
            : base(BuildMessage(mensagens))
        {
            HttpStatusCode = httpStatusCode;
            Mensagens = mensagens ?? Array.Empty<MensagemErro>();
        }

        private static string BuildMessage(IReadOnlyList<MensagemErro> mensagens)
        {
            if (mensagens == null || mensagens.Count == 0)
            {
                return "Erro de negócio retornado pela API do banco.";
            }

            var primeira = mensagens[0];
            return $"[{primeira.Codigo}] {primeira.Mensagem}";
        }
    }
}

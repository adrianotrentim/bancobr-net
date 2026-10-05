namespace BancoBr.API.Sicoob.Errors
{
    /// <summary>
    /// Códigos de erro de negócio do Sicoob (campo "codigo" em MensagemErro) tratados
    /// explicitamente pelos clients, em vez de propagados como exceção genérica.
    /// </summary>
    internal static class SicoobErrorCodes
    {
        /// <summary>
        /// "O idempotency já foi utilizado com sucesso em outra execução" — o pagamento já foi
        /// efetivado numa tentativa anterior (mesma chave de idempotência em boleto, ou mesma
        /// transação em convênio); o client deve recuperar o comprovante já existente em vez de
        /// propagar o erro.
        /// </summary>
        public const string IdempotencyJaUtilizado = "10272";
    }
}

using System;

namespace BancoBr.API.Core.Models
{
    /// <summary>
    /// Constrói o valor exigido pelo header x-idempotency-key do Sicoob, exigido apenas na
    /// inclusão (pagamento): {numeroCooperativa (até 4 dígitos)}-{numeroContaCorrente (até 14
    /// dígitos)}-{UUID}. Ex.: 4342-8901234-550e8400-e29b-41d4-a716-446655440000. O UUID usado é
    /// o idLancamento informado pelo ERP — duas chamadas para o mesmo lançamento produzem a
    /// mesma key, permitindo que o Sicoob deduplique pagamentos repetidos (ex.: duplo clique do
    /// usuário no ERP).
    /// </summary>
    internal static class IdempotencyKey
    {
        private const int MaxDigitosCooperativa = 4;
        private const int MaxDigitosContaCorrente = 14;

        public static string New(int numeroCooperativa, long numeroContaCorrente, Guid idLancamento)
        {
            if (idLancamento == Guid.Empty)
                throw new ArgumentException("idLancamento é obrigatório.", nameof(idLancamento));

            ValidateDigitos(numeroCooperativa, MaxDigitosCooperativa, nameof(numeroCooperativa));
            ValidateDigitos(numeroContaCorrente, MaxDigitosContaCorrente, nameof(numeroContaCorrente));

            return $"{numeroCooperativa}-{numeroContaCorrente}-{idLancamento:D}";
        }

        private static void ValidateDigitos(long valor, int maxDigitos, string paramName)
        {
            if (valor < 0)
                throw new ArgumentException($"{paramName} não pode ser negativo.", paramName);

            if (valor.ToString().Length > maxDigitos)
                throw new ArgumentException($"{paramName} excede o limite de {maxDigitos} dígitos.", paramName);
        }
    }
}

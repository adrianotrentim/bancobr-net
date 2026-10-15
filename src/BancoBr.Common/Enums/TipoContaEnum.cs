using System.ComponentModel;

namespace BancoBr.Common.Enums
{
    public enum TipoContaEnum
    {
        [Description("Conta Corrente")]
        ContaCorrente = 0,
        [Description("Conta Poupança")]
        ContaPoupanca = 1,
        /// <summary>Conta de pagamento (instituições de pagamento). Só usada pelo Pix por agência e conta do Santander.</summary>
        [Description("Conta de Pagamento")]
        ContaPagamento = 2
    }
}
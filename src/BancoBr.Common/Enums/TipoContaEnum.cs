using System.ComponentModel;

namespace BancoBr.Common.Enums
{
    public enum TipoContaEnum
    {
        [Description("Conta Corrente")]
        ContaCorrente = 0,
        [Description("Conta Poupança")]
        ContaPoupanca = 1
    }
}
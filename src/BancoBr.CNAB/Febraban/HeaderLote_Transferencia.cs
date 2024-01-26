/*
 * Pagamento Através de Crédito em Conta, Cheque, OP, DOC, TED ou Pagamento com Autenticação
 */

using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban
{
    public class HeaderLote_Transferencia : HeaderLote
    {
        public HeaderLote_Transferencia(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 46;
            Operacao = "C";
        }

        [CampoCNAB(223, 2)]
        public virtual LocalDebitoEnum LocalDebito { get; set; }

        [CampoCNAB(225, 6)]
        public override string CNAB2 { get; set; }
    }
}

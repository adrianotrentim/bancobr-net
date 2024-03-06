/*
 * Pagamento Através de Crédito em Conta, Cheque, OP, DOC, TED ou Pagamento com Autenticação
 */

using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban
{
    public class HeaderLote_TransferenciaConvenio : HeaderLote
    {
        public HeaderLote_TransferenciaConvenio(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(223, 2)]
        public virtual LocalDebitoEnum LocalDebito { get; set; }

        [CampoCNAB(225, 6)]
        public override string CNAB2 { get; set; }
    }
}

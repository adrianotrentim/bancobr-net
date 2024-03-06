using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Itau
{
    public sealed class HeaderLote : Febraban.HeaderLote_TransferenciaConvenio
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 40;
            Operacao = "C";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string Convenio { get; set; }

        [CampoCNAB(true)]
        private new string DVAgencia { get; set; }

        [CampoCNAB(true)]
        private new string DVConta { get; set; }

        [CampoCNAB(true)]
        private new LocalDebitoEnum LocalDebito { get; set; }

        #endregion

        [CampoCNAB(33, 4)]
        public string IdentificacaoLancamento { get; set; }

        [CampoCNAB(37, 16)]
        public override string CNAB2 { get; set; }
        
        [CampoCNAB(58, 1)]
        public string CNAB3 { get; set; }

        [CampoCNAB(71, 1)]
        public string CNAB4 { get; set; }

        [CampoCNAB(223, 8)]
        public string CNAB5 { get; set; }
    }
}

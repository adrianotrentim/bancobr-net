using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Itau
{
    public class HeaderLote_DocTedPixCredConta : Febraban.HeaderLote
    {
        public HeaderLote_DocTedPixCredConta(Common.Instances.Banco banco)
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
        private new string DVAgenciaConta { get; set; }

        [CampoCNAB(true)]
        private new string Informacao1 { get; set; }

        [CampoCNAB(true)]
        private new TipoLancamentoEnum TipoLancamento { get; set; }

        #endregion

        [CampoCNAB(33, 4)]
        public string IdentificacaoLancamento { get; set; }

        [CampoCNAB(37, 16)]
        public override string CNAB2 { get; set; }
        
        [CampoCNAB(58, 1)]
        public string CNAB3 { get; set; }

        [CampoCNAB(71, 1)]
        public string CNAB4 { get; set; }

        [CampoCNAB(72, 1)]
        public override string DVConta { get; set; }

        [CampoCNAB(103, 30)]
        public string FinalidadeLote { get; set; }

        [CampoCNAB(133, 10)]
        public string HistoricoContaCorrente { get; set; }

        [CampoCNAB(223, 8)]
        public string CNAB5 { get; set; }
    }
}

using BancoBr.Common.Attributes;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Itau
{
    public class HeaderArquivo : Febraban.HeaderArquivo
    {
        public HeaderArquivo(Common.Instances.Banco banco, Pessoa empresaCedente, int numeroRemessa)
            : base(banco, empresaCedente, numeroRemessa)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string DVAgencia { get; set; }

        [CampoCNAB(true)]
        private new string DVAgenciaConta { get; set; }

        [CampoCNAB(true)]
        private new int NumeroSequencialArquivo { get; set; }

        [CampoCNAB(true)]
        private new string ReservadoBanco { get; set; }

        [CampoCNAB(true)]
        private new string ReservadoEmpresa { get; set; }

        #endregion

        [CampoCNAB(9, 6)]
        public new string CNAB1 { get; set; }

        [CampoCNAB(15, 3)]
        public new int VersaoArquivo { get; set; }

        [CampoCNAB(33, 20)]
        public new string CNAB2 { get; set; }
        
        [CampoCNAB(53, 5)]
        public new int NumeroAgencia { get; set; }

        [CampoCNAB(58, 1)]
        public new string CNAB3 { get; set; }

        [CampoCNAB(59, 12)]
        public new int NumeroConta { get; set; }

        [CampoCNAB(71, 1)]
        public new string CNAB4 { get; set; }

        [CampoCNAB(72, 1)]
        public new string DVConta { get; set; }

        [CampoCNAB(133, 10)]
        public new string CNAB5 { get; set; }

        [CampoCNAB(158, 9)]
        public new int CNAB6 { get; set; }

        [CampoCNAB(172, 69)]
        public string CNAB7 { get; set; }
    }
}

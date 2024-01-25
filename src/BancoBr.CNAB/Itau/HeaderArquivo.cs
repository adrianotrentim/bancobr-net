using BancoBr.Common.Attributes;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Itau
{
    public class HeaderArquivo : Febraban.HeaderArquivo
    {
        public HeaderArquivo(Common.Instances.Banco banco, Correntista correntista, int numeroRemessa)
            : base(banco, correntista, numeroRemessa)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string Convenio { get; set; }

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
        public override string CNAB1 { get; set; }

        [CampoCNAB(15, 3)]
        public override int VersaoArquivo { get; set; }

        [CampoCNAB(33, 20)]
        public override string CNAB2 { get; set; }
        
        [CampoCNAB(53, 5)]
        public override int NumeroAgencia { get; set; }

        [CampoCNAB(58, 1)]
        public override string CNAB3 { get; set; }

        [CampoCNAB(59, 12)]
        public override int NumeroConta { get; set; }

        [CampoCNAB(71, 1)]
        public string CNAB4 { get; set; }

        [CampoCNAB(72, 1)]
        public override string DVConta { get; set; }

        [CampoCNAB(133, 10)]
        public string CNAB5 { get; set; }

        [CampoCNAB(158, 9)]
        public int CNAB6 { get; set; }

        [CampoCNAB(172, 69)]
        public string CNAB7 { get; set; }
    }
}

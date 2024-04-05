using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Itau
{
    public class SegmentoA : Febraban.SegmentoA_Transferencia
    {
        public SegmentoA(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new decimal QuantidadeMoeda { get; set; }

        #endregion

        [CampoCNAB(105, 8)]
        public string IdentificacaoSPB { get; set; }

        [CampoCNAB(113, 2)]
        public string IdentificacaoTransferencia { get; set; }

        [CampoCNAB(115, 5)]
        public int CNAB2 { get; set; }

        [CampoCNAB(135, 15)]
        public override string NumeroDocumentoBanco { get; set; }

        [CampoCNAB(150, 5)]
        public string CNAB3 { get; set; }

        [CampoCNAB(178, 20)]
        public override string Informacao2 { get; set; }

        [CampoCNAB(198, 6)]
        public int NumeroDocumentoRetorno { get; set; }

        [CampoCNAB(204, 14)]
        public long InscricaoFavorecido { get; set; }
    }
}

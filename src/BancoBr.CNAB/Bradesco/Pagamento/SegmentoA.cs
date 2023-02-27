using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Bradesco.Pagamento
{
    public class SegmentoA : Febraban.Pagamento.SegmentoA
    {
        public SegmentoA(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "A";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string CodigoFinalidadeComplementar { get; set; }

        #endregion

        [CampoCNAB(225, 5)]
        public new string CNAB1 { get; set; }
    }
}
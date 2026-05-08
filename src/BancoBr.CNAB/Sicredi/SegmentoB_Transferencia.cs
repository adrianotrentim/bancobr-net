using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Sicredi
{
    public class SegmentoB_Transferencia : Febraban.SegmentoB_Transferencia
    {
        public SegmentoB_Transferencia(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new int IdentificacaoBancoSPB { get; set; }

        #endregion

        /// <summary>
        /// Utilizando o tipo de serviço Folha de Pagamento deixar este campo em branco. 
        /// </summary>

        [CampoCNAB(233, 8)]
        public string IdentificacaoBancoSPBBranco
        {
            get
            {
                return string.Empty;
            }
        }


    }
}
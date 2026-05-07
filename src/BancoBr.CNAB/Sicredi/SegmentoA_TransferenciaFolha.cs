using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoA_TransferenciaFolha : SegmentoA_Transferencia
    {
        public SegmentoA_TransferenciaFolha(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]        
        private new FinalidadeTEDEnum CodigoFinalidadeTED { get; set; }

        #endregion


        /// <summary>
        /// Utilizando o tipo de serviço Folha de Pagamento deixar este campo em branco. 
        /// </summary>

        [CampoCNAB(220, 5)]
        public string CodigoFinalidadeTEDBranco
        {
            get
            {
                return string.Empty; 
            }
        }
    }
}
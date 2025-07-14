using System.Collections.Generic;
using System.Linq;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Bradesco
{
    public class SegmentoB_PIX : Febraban.SegmentoB_PIX
    {
        public SegmentoB_PIX(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)] 
        private new int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(true)] 
        private new int IdentificacaoBancoSPB { get; set; }

        #endregion
        
        [CampoCNAB(231, 10)]
        public string Ocorrencias { get; set; }

        [CampoCNAB(true)]
        public List<Ocorrencia> ListaOcorrenciasRetorno
        {
            get
            {
                var ocorrencias = Ocorrencias;
                var listaOcorrencias = new List<Ocorrencia>();

                while (true)
                {
                    if (string.IsNullOrEmpty(ocorrencias))
                        break;

                    var ocorrencia = ocorrencias.Substring(0, 2);

                    listaOcorrencias.Add(CodigoOcorrenciasRetorno.Ocorrencias.FirstOrDefault(t => t.Codigo == ocorrencia));

                    ocorrencias = ocorrencias.Substring(2, ocorrencias.Length - 2);
                }

                return listaOcorrencias;
            }
        }
    }
}
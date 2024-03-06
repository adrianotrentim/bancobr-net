using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Instances;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Itau
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

        [CampoCNAB(true)]
        private string TX_ID { get; set; }

        #endregion

        [CampoCNAB(33, 30)]
        public string CNAB1 { get; set; }
        
        [CampoCNAB(63, 65)]
        public override string InformacaoEntreUsuarios { get; set; }

        [CampoCNAB(128, 100)]
        public override string ChavePIX { get; set; }

        [CampoCNAB(228, 3)]
        public string CNAB2 { get; set; }

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
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Itau
{
    public class SegmentoB_Transferencia : Febraban.SegmentoB_Transferencia
    {
        public SegmentoB_Transferencia(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new int CodigoUGCentralizadora { get; set; }

        [CampoCNAB(true)]
        private new int IdentificacaoBancoSPB { get; set; }

        [CampoCNAB(true)]
        private new DateTime Vencimento { get; set; }

        [CampoCNAB(true)]
        private new decimal ValorDocumento { get; set; }

        [CampoCNAB(true)]
        private new decimal Abatimento { get; set; }

        [CampoCNAB(true)]
        private new decimal Desconto { get; set; }

        [CampoCNAB(true)]
        private new decimal Mora { get; set; }

        [CampoCNAB(true)]
        private new decimal Multa { get; set; }

        [CampoCNAB(true)]
        private new string DocumentoFavorecido { get; set; }

        [CampoCNAB(true)]
        private new int AvisoFavorecido { get; set; }

        #endregion

        [CampoCNAB(128, 100)]
        public string Email { get; set; }

        [CampoCNAB(228, 3)]
        public string CNAB2 { get; set; }

        [CampoCNAB(231, 10)]
        public virtual string Ocorrencias { get; set; }

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
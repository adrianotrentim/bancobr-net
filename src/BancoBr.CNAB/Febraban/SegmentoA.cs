using System;
using System.Collections.Generic;
using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.CNAB.Core;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoA : RegistroDetalheBase
    {
        public SegmentoA(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "A";
        }

        [CampoCNAB(15, 1)]
        public virtual TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(16, 2)]
        public virtual CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }

        [CampoCNAB(18, 3)]
        public virtual int CamaraCentralizadora { get; set; }

        /*
         *
         * 21 a 43 - Implementar nas classes Herdadas
         *
         */

        [CampoCNAB(44, 30)]
        public virtual string NomeFavorecido { get; set; }

        [CampoCNAB(74, 20)]
        public virtual string NumeroDocumentoEmpresa { get; set; }

        [CampoCNAB(94, 8)]
        public virtual DateTime DataPagamento { get; set; }

        [CampoCNAB(102, 3)]
        public virtual string TipoMoeda { get; set; }

        [CampoCNAB(105, 15)]
        public virtual decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(120, 15)]
        public virtual decimal ValorPagamento { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(135, 20)]
        public virtual string NumeroDocumentoBanco { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(155, 8)]
        public virtual DateTime DataRealPagamento { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(163, 15)]
        public virtual decimal ValorRealPagamento { get; set; }

        [CampoCNAB(178, 40)]
        public virtual string Informacao2 { get; set; }

        /// <summary>
        /// Verificar Nota P005 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(218, 2)]
        public virtual string CodigoFinalidadeDOC { get; set; }

        /*
         *
         * 220 a 225 - Implementar nas classes Herdadas
         *
         */

        [CampoCNAB(225, 2)]
        public virtual string CodigoFinalidadeComplementar { get; set; }

        [CampoCNAB(227, 3)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(230, 1)]
        public virtual AvisoFavorecidoEnum AvisoFavorecido { get; set; }

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
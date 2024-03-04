using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoO : RegistroDetalheBase
    {
        public SegmentoO(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "O";
        }

        [CampoCNAB(15, 1)]
        public virtual TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(16, 2)]
        public virtual CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }

        [CampoCNAB(18, 44)]
        public virtual string CodigoBarra { get; set; }

        [CampoCNAB(62, 30)]
        public virtual string NomeBeneficiario { get; set; }

        [CampoCNAB(92, 8)]
        public virtual DateTime DataVencimento { get; set; }

        [CampoCNAB(100, 8)]
        public virtual DateTime DataPagamento { get; set; }

        [CampoCNAB(108, 15)]
        public virtual decimal ValorPagamento { get; set; }

        [CampoCNAB(123, 20)]
        public virtual string NumeroDocumentoEmpresa { get; set; }

        [CampoCNAB(143, 20)]
        public virtual string NumeroDocumentoBanco { get; set; }

        [CampoCNAB(163, 68)]
        public virtual string CNAB1 { get; set; }

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

                    listaOcorrencias.Add(CodigoOcorrenciasRetorno.Ocorrencias.First(t => t.Codigo == ocorrencia));

                    ocorrencias = ocorrencias.Substring(2, ocorrencias.Length - 2);
                }

                return listaOcorrencias;
            }
        }
    }
}

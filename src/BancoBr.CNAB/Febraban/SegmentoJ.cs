using System;
using System.Collections.Generic;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoJ : RegistroDetalheBase
    {
        public SegmentoJ(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "J";
        }

        [CampoCNAB(15, 1)]
        public virtual TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(16, 2)]
        public virtual CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }

        [CampoCNAB(18, 3)]
        public virtual int BancoCodigoBarra { get; set; }

        [CampoCNAB(21, 1)]
        public virtual int MoedaCodigoBarra { get; set; }

        [CampoCNAB(22, 1)]
        public virtual int DVCodigoBarra { get; set; }

        [CampoCNAB(23, 4)]
        public virtual int FatorVencimentoCodigoBarra { get; set; }

        [CampoCNAB(27, 10)]
        public virtual decimal ValorCodigoBarra { get; set; }

        [CampoCNAB(37, 25, '0', AlinhamentoPreenchimentoEnum.PreencherAEsquerda)]
        public virtual string CampoLivreCodigoBarra { get; set; }

        [CampoCNAB(62, 30)]
        public virtual string NomeBeneficiario { get; set; }

        [CampoCNAB(92, 8)]
        public virtual DateTime DataVencimento { get; set; }

        [CampoCNAB(100, 15)]
        public virtual decimal ValorTitulo { get; set; }

        [CampoCNAB(115, 15)]
        public virtual decimal ValorDesconto { get; set; }

        [CampoCNAB(130, 15)]
        public virtual decimal ValorAcrescimo { get; set; }

        [CampoCNAB(145, 8)]
        public virtual DateTime DataPagamento { get; set; }

        [CampoCNAB(153, 15)]
        public virtual decimal ValorPagamento { get; set; }

        [CampoCNAB(168, 15)]
        public virtual decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(183, 20)]
        public virtual string NumeroDocumentoEmpresa { get; set; }

        [CampoCNAB(203, 20)]
        public virtual string NumeroDocumentoBanco { get; set; }

        [CampoCNAB(223, 2)]
        public virtual int CodigoMoeda { get; set; }

        [CampoCNAB(225, 6)]
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

                    listaOcorrencias.Add(new Ocorrencia(ocorrencia, CodigoOcorrenciasRetorno.Ocorrencias[ocorrencia]));

                    ocorrencias = ocorrencias.Substring(2, ocorrencias.Length - 2);
                }

                return listaOcorrencias;
            }
        }
    }
}

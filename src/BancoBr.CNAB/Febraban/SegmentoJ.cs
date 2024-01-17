using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

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

        [CampoCNAB(18, 44)]
        public virtual string CodigoBarra { get; set; }

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

        [CampoCNAB(168, 5)]
        public virtual int QuantidadeMoeda { get; set; }

        [CampoCNAB(183, 20)]
        public virtual string CodigoDocumentoNaEmpresa { get; set; }

        [CampoCNAB(203, 20)]
        public virtual string CodigoDocumentoNoBanco { get; set; }

        [CampoCNAB(223, 2)]
        public virtual int CodigoMoeda { get; set; }

        [CampoCNAB(225, 6)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(231, 10)]
        public virtual string CodigoOcorrencia { get; set; }
    }
}

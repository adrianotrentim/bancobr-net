using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.CNAB.Itau
{
    public class SegmentoO : Febraban.SegmentoO
    {
        public SegmentoO(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoMoeda = "REA";
        }

        [CampoCNAB(18, 48)]
        public override string CodigoBarra { get; set; }

        [CampoCNAB(66, 30)]
        public override string NomeBeneficiario { get; set; }

        [CampoCNAB(96, 8)]
        public override DateTime DataVencimento { get; set; }

        [CampoCNAB(104, 3)]
        public string TipoMoeda { get; set; }

        [CampoCNAB(107, 15)]
        public decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(122, 15)]
        public override decimal ValorPagamento { get; set; }

        [CampoCNAB(137, 8)]
        public override DateTime DataPagamento { get; set; }

        [CampoCNAB(145, 15)]
        public decimal ValorEfetivacaoPagamento { get; set; }

        [CampoCNAB(160, 3)]
        public override string CNAB1 { get; set; }

        [CampoCNAB(163, 9)]
        public string NotaFiscal { get; set; }

        [CampoCNAB(172, 3)]
        public string CNAB2 { get; set; }

        [CampoCNAB(175, 20)]
        public override string NumeroDocumentoEmpresa { get; set; }

        [CampoCNAB(195, 21)]
        public string CNAB3 { get; set; }

        [CampoCNAB(216, 15)]
        public override string NumeroDocumentoBanco { get; set; }

        
    }
}

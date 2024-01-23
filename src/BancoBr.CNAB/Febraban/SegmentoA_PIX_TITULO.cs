﻿using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoA_PIX_TITULO : SegmentoA
    {
        public SegmentoA_PIX_TITULO(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(15, 1)]
        public virtual TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(16, 2)]
        public virtual CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }

        [CampoCNAB(18, 11)]
        public int ZERO1 { get; }

        [CampoCNAB(29, 1)]
        public string ALFA1 { get; }

        [CampoCNAB(30, 12)]
        public int ZERO2 { get; }

        [CampoCNAB(42, 1)]
        public string ALFA2 { get; }

        [CampoCNAB(43, 1)]
        public string ALFA3 { get; }

        /// <summary>
        /// Verificar Nota P011 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(220, 5)]
        public string ALFA4 { get; set; }
    }
}
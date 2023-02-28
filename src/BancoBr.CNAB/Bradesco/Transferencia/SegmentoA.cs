﻿using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Bradesco.Transferencia
{
    public class SegmentoA : Febraban.Transferencia.SegmentoA
    {
        public SegmentoA(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "A";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new string CodigoFinalidadeComplementar { get; set; }

        #endregion

        [CampoCNAB(225, 5)]
        public override string CNAB1 { get; set; }
    }
}
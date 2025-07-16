using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.CNAB.Itau
{
    public class SegmentoJ52_PIX_QRCODE : Febraban.SegmentoJ52_PIX_QRCODE
    {
        public SegmentoJ52_PIX_QRCODE(Common.Instances.Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(132, 77)]
        public new string URL_ChavePIX { get; set; }

        [CampoCNAB(209, 32)]
        public new string CodigoIdentificacaoQRCODE { get; set; }
    }
}

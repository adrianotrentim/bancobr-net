﻿using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco.Pagamento
{
    public class HeaderLote : Febraban.Pagamento.HeaderLote
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 40;
            Operacao = "C";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new TipoFormaPagamentoEnum IndicativoFormaPagamento { get; set; }

        #endregion

        [CampoCNAB(26, 8)]
        public new string CNAB2 { get; set; }
    }
}

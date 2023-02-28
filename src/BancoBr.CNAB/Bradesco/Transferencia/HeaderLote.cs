using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Bradesco.Transferencia
{
    public class HeaderLote : Febraban.Transferencia.HeaderLote
    {
        public HeaderLote(Common.Instances.Banco banco)
            : base(banco)
        {
            VersaoLote = 40;
            Operacao = "C";
        }

        #region ::. Propriedades Desativadas .::

        [CampoCNAB(true)]
        private new TipoLancamentoEnum TipoLancamento { get; set; }

        #endregion

        [CampoCNAB(223, 8)]
        public override string CNAB2 { get; set; }
    }
}

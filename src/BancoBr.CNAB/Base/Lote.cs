using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class Lote
    {
        private int _numeroRegistro = 1;

        public Lote()
        {
            Detalhe = new List<RegistroDetalheBase>();
        }

        public HeaderLoteBase Header { get; set; }
        public List<RegistroDetalheBase> Detalhe { get; set; }
        public TrailerLoteBase Trailer { get; set; }

        #region ::. Bloco de Transferências .::

        public void NovaTransferencia(Transferencia titulo)
        {
            Detalhe.AddRange(((Banco)Header.Banco).NovaTransferencia(titulo, Header.LoteServico, _numeroRegistro));

            _numeroRegistro = Detalhe.Count + 1;
        }

        #endregion
    }
}

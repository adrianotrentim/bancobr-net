using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class Lote
    {
        private int _numeroRegistro = 1;

        public Lote()
        {
            Registros = new List<RegistroDetalheBase>();
        }

        public HeaderLoteBase Header { get; set; }
        public List<RegistroDetalheBase> Registros { get; set; }
        public TrailerLoteBase Trailer { get; set; }

        #region ::. Bloco de Pagamentos .::

        public void NovoPagamento(Pagamento titulo)
        {
            Registros.AddRange(((Banco)Header.Banco).NovoPagamento(titulo, Header.LoteServico, _numeroRegistro));

            _numeroRegistro = Registros.Count + 1;
        }

        #endregion
    }
}

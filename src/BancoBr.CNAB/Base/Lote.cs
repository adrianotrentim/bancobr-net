using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class Lote
    {
        private int _numeroRegistro = 0;

        public Lote()
        {
            Registros = new List<RegistroBase>();
        }

        public HeaderLoteBase Header { get; set; }
        public List<RegistroBase> Registros { get; set; }
        public TrailerLoteBase Trailer => new TrailerLoteBase(this, Registros);

        #region ::. Bloco de Pagamentos .::

        public void NovoPagamento(Pagamento titulo)
        {
            _numeroRegistro++;

            Registros.AddRange(((Banco)Header.Banco).NovoPagamento(titulo, _numeroRegistro));
        }

        #endregion
    }
}

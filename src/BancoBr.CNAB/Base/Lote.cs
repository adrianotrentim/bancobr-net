using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class Lote
    {
        public Lote()
        {
            Registros = new List<Registro>();
        }

        public HeaderLote Header { get; set; }
        public List<Registro> Registros { get; set; }
        public TrailerLote Trailer => new TrailerLote(this, Registros);

        #region ::. Bloco de Pagamentos .::

        public void AddPagamento(Titulo titulo)
        {
            Registros.AddRange(((Banco)Header.Banco).GetPagamento(titulo));
        }

        #endregion
    }
}

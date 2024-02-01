using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class Lote
    {
        private int _numeroRegistro = 0;

        public Lote()
        {
            Detalhe = new List<RegistroDetalheBase>();
        }

        public HeaderLoteBase Header { get; set; }
        public List<RegistroDetalheBase> Detalhe { get; set; }
        public TrailerLoteBase Trailer { get; set; }

        internal void AdicionarMovimento(Movimento titulo)
        {
            Detalhe.AddRange(((Banco)Header.Banco).NovoMovimento(titulo, Header.LoteServico, _numeroRegistro));

            _numeroRegistro = Detalhe.Count;
        }
    }
}

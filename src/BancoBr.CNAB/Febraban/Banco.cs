using System.Collections.Generic;
using BancoBr.Common.Interfaces;

namespace BancoBr.CNAB.Febraban
{
    public class Banco : IBanco
    {
        public Banco()
        {
            HeaderArquivo = new HeaderArquivo();
            HeaderArquivo.CodigoBanco = Codigo;

            HeaderLote = new List<HeaderLote>();
        }

        public int Codigo { get; set; }
        public string Nome { get; set; }

        public HeaderArquivo HeaderArquivo { get; set; }
        public List<HeaderLote> HeaderLote { get; set; }
    }
}

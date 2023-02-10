using System.Collections.Generic;
using BancoBr.Common.Interfaces;

namespace BancoBr.CNAB.Febraban
{
    public class Banco : IBanco
    {
        protected Banco(int codigo, string nome)
        {
            Codigo = codigo;
            Nome = nome;

            HeaderArquivo = new HeaderArquivo();
            HeaderArquivo.CodigoBanco = codigo;

            HeaderLote = new List<HeaderLote>();
        }

        public int Codigo { get; set; }
        public string Nome { get; set; }

        public HeaderArquivo HeaderArquivo { get; set; }
        public List<HeaderLote> HeaderLote { get; set; }
    }
}

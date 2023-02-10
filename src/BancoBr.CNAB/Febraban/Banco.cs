using System.Collections.Generic;

namespace BancoBr.CNAB.Febraban
{
    public class Banco : Common.Instances.Banco
    {
        protected Banco(int codigo, string nome)
            : base(codigo, nome)
        {
            HeaderArquivo = new HeaderArquivo();
            HeaderArquivo.CodigoBanco = codigo;

            HeaderLote = new List<HeaderLote>();
        }

        public HeaderArquivo HeaderArquivo { get; set; }
        public List<HeaderLote> HeaderLote { get; set; }
    }
}

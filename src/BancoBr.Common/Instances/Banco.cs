using BancoBr.Common.Enums;
using BancoBr.Common.Interfaces;

namespace BancoBr.Common.Instances
{
    public abstract class Banco : IBanco
    {
        protected Banco(Correntista empresa, int codigo, string nome, int versaoArquivo)
        {
            Empresa = empresa;
            Codigo = codigo;
            Nome = nome;
            VersaoArquivo = versaoArquivo;
        }

        public Correntista Empresa { get; set; }
        public int Codigo { get; set; }
        public string Nome { get; set; }
        public int VersaoArquivo { get; set; }

        public override string ToString()
        {
            return $"{Codigo} - {Nome}";
        }
    }
}

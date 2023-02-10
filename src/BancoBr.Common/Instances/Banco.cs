using BancoBr.Common.Interfaces;
using System.Collections.Generic;

namespace BancoBr.Common.Instances
{
    public class Banco : IBanco
    {
        protected Banco(int codigo, string nome)
        {
            Codigo = codigo;
            Nome = nome;
        }

        public int Codigo { get; set; }
        public string Nome { get; set; }

        public override string ToString()
        {
            return $"[({Codigo}) {Nome}";
        }
    }
}

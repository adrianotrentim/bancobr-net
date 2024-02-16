namespace BancoBr.Common.Instances
{
    public class Ocorrencia
    {
        public Ocorrencia(string codigo, string descricao)
        {
            Codigo = codigo;
            Descricao = descricao;
        }

        public string Codigo { get; }
        public string Descricao { get; }

        public override string ToString()
        {
            return $"{Codigo} - {Descricao}";
        }
    }
}
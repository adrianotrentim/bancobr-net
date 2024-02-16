namespace BancoBr.Common.Instances
{
    public class Ocorrencia
    {
        public Ocorrencia(string codigo, string descricao, string explicacao = null)
        {
            Codigo = codigo;
            Descricao = descricao;
            Explicacao = explicacao;
        }

        public string Codigo { get; }
        public string Descricao { get; }
        public string Explicacao { get; }

        public override string ToString()
        {
            return $"{Codigo} - {Descricao}";
        }
    }
}
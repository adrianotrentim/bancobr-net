using BancoBr.Common.Enums;

namespace BancoBr.Common.Instances
{
    public abstract class Pessoa
    {
        public TipoInscricaoCPFCNPJEnum TipoPessoa { get; set; }
        public string CPF_CNPJ { get; set; }
        public string Nome { get; set; }
        public string Endereco { get; set; }
        public string NumeroEndereco { get; set; }
        public string ComplementoEndereco { get; set; }
        public int CEP { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string UF { get; set; }
    }

    public class Favorecido : Pessoa
    {
        
    }

    public class Correntista : Pessoa
    {
        public int Banco { get; set; }
        public string Convenio { get; set; }
        public int NumeroAgencia { get; set; }
        public string DVAgencia { get; set; }
        public int NumeroConta { get; set; }
        public string DVConta { get; set; }
    }
}
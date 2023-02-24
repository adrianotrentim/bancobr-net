using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class HeaderLote : HeaderLoteBase
    {
        public HeaderLote(Common.Instances.Banco banco) 
            : base(banco)
        {
            VersaoLote = 46;
            Operacao = "C";
        }

        [CampoCNAB(6, 2)]
        public FormaPagamentoEnum FormaPagamento { get; set; }

        [CampoCNAB(7, 3)]
        public int VersaoLote { get; set; }

        [CampoCNAB(8, 1)]
        public string CNAB1 { get; set; }

        [CampoCNAB(9, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoEmpresa { get; set; }

        [CampoCNAB(10, 14)]
        public long InscricaoEmpresa { get; set; }

        [CampoCNAB(11, 20)]
        public string Convenio { get; set; }

        [CampoCNAB(12, 5)]
        public int NumeroAgencia { get; set; }

        [CampoCNAB(13, 1)]
        public string DVAgencia { get; set; }

        [CampoCNAB(14, 12)]
        public int NumeroConta { get; set; }

        [CampoCNAB(15, 1)]
        public string DVConta { get; set; }

        [CampoCNAB(16, 1)]
        public string DVAgenciaConta { get; set; }

        [CampoCNAB(17, 30)]
        public string NomeEmpresa { get; set; }

        [CampoCNAB(18, 40)]
        public string Informacao1 { get; set; }

        [CampoCNAB(19, 30)]
        public string EnderecoEmpresa { get; set; }

        [CampoCNAB(20, 5)]
        public string NumeroEnderecoEmpresa { get; set; }

        [CampoCNAB(21, 15)]
        public string ComplementoEnderecoEmpresa { get; set; }

        [CampoCNAB(22, 20)]
        public string CidadeEmpresa { get; set; }

        [CampoCNAB(23, 8)]
        public int CEPEmpresa { get; set; }

        [CampoCNAB(25, 2)]
        public string UFEmpresa { get; set; }

        [CampoCNAB(26, 2)]
        public TipoLancamentoEnum TipoLancamento { get; set; }

        [CampoCNAB(27, 6)]
        public string CNAB2 { get; set; }

        [CampoCNAB(28, 10)]
        public string Ocorrencias { get; set; }
    }
}

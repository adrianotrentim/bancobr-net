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

        [CampoCNAB(12, 2)]
        public FormaPagamentoEnum FormaPagamento { get; set; }

        [CampoCNAB(14, 3)]
        public int VersaoLote { get; set; }

        [CampoCNAB(17, 1)]
        public string CNAB1 { get; set; }

        [CampoCNAB(18, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoEmpresa { get; set; }

        [CampoCNAB(19, 14)]
        public long InscricaoEmpresa { get; set; }

        [CampoCNAB(33, 20)]
        public string Convenio { get; set; }

        [CampoCNAB(53, 5)]
        public int NumeroAgencia { get; set; }

        [CampoCNAB(58, 1)]
        public string DVAgencia { get; set; }

        [CampoCNAB(59, 12)]
        public int NumeroConta { get; set; }

        [CampoCNAB(71, 1)]
        public string DVConta { get; set; }

        [CampoCNAB(72, 1)]
        public string DVAgenciaConta { get; set; }

        [CampoCNAB(73, 30)]
        public string NomeEmpresa { get; set; }

        [CampoCNAB(103, 40)]
        public string Informacao1 { get; set; }

        [CampoCNAB(143, 30)]
        public string EnderecoEmpresa { get; set; }

        [CampoCNAB(173, 5)]
        public string NumeroEnderecoEmpresa { get; set; }

        [CampoCNAB(178, 15)]
        public string ComplementoEnderecoEmpresa { get; set; }

        [CampoCNAB(193, 20)]
        public string CidadeEmpresa { get; set; }

        [CampoCNAB(213, 8)]
        public int CEPEmpresa { get; set; }

        [CampoCNAB(221, 2)]
        public string UFEmpresa { get; set; }

        [CampoCNAB(223, 2)]
        public TipoLancamentoEnum TipoLancamento { get; set; }

        [CampoCNAB(225, 6)]
        public string CNAB2 { get; set; }

        [CampoCNAB(231, 10)]
        public string Ocorrencias { get; set; }
    }
}

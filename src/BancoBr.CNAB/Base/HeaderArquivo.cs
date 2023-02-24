using System;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class HeaderArquivo : RegistroBase
    {
        public HeaderArquivo(Common.Instances.Banco banco, Pessoa empresaCedente, int numeroRemessa)
            : base(banco)
        {
            LoteServico = 0;
            TipoRegistro = 0;
            VersaoArquivo = 103;
            NumeroSequencialArquivo = numeroRemessa;
            DataGeracao = DateTime.Now;
            HoraGeracao = DateTime.Now;
            CodigoRemessaRetorno = TipoArquivoEnum.Remessa;
            VersaoArquivo = banco.VersaoArquivo;

            if (empresaCedente != null)
            {
                TipoInscricaoCpfcnpj = empresaCedente.TipoPessoa;
                InscricaoEmpresa = long.Parse(empresaCedente.CPF_CNPJ.JustNumbers());
                Convenio = empresaCedente.Convenio;
                NumeroAgencia = empresaCedente.NumeroAgencia;
                DVAgencia = empresaCedente.DVAgencia;
                NumeroConta = empresaCedente.NumeroConta;
                DVConta = empresaCedente.DVConta.Substring(0, 1);

                if (empresaCedente.DVConta.Length >= 2)
                    DVAgenciaConta = empresaCedente.DVConta.Substring(1, 1);
            }
        }

        [CampoCNAB(4, 9)]
        public string CNAB1 { get; set; }

        [CampoCNAB(5, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoCpfcnpj { get; set; }

        [CampoCNAB(6, 14)]
        public long InscricaoEmpresa { get; set; }

        [CampoCNAB(7, 20)]
        public string Convenio { get; set; }

        [CampoCNAB(8, 5)]
        public int NumeroAgencia { get; set; }

        [CampoCNAB(9, 1)]
        public string DVAgencia { get; set; }

        [CampoCNAB(10, 12)]
        public int NumeroConta { get; set; }

        [CampoCNAB(11, 1)]
        public string DVConta { get; set; }

        [CampoCNAB(12, 1)]
        public string DVAgenciaConta { get; set; }

        [CampoCNAB(13, 30)]
        public string NomeEmpresa { get; set; }

        [CampoCNAB(14, 30)]
        public string NomeBanco { get; set; }

        [CampoCNAB(15, 10)]
        public string CNAB2 { get; set; }

        [CampoCNAB(16, 1)]
        public TipoArquivoEnum CodigoRemessaRetorno { get; set; }

        [CampoCNAB(17, 8, "ddMMyyyy")]
        public DateTime DataGeracao { get; set; }

        [CampoCNAB(18, 6, "HHmmss")]
        public DateTime HoraGeracao { get; set; }

        [CampoCNAB(19, 6)]
        public int NumeroSequencialArquivo { get; set; }

        [CampoCNAB(20, 3)]
        public int VersaoArquivo { get; set; }

        [CampoCNAB(21, 5)]
        public int DensidadeArquivo { get; set; }

        [CampoCNAB(22, 20)]
        public string ReservadorBanco { get; set; }

        [CampoCNAB(23, 20)]
        public string ReservadorEmpresa { get; set; }

        [CampoCNAB(24, 29)]
        public string CNAB3 { get; set; }
    }
}

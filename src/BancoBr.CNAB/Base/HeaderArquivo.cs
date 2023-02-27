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
            TipoRemessaRetorno = TipoArquivoEnum.Remessa;
            VersaoArquivo = banco.VersaoArquivo;

            if (empresaCedente != null)
            {
                TipoInscricaoCpfcnpj = empresaCedente.TipoPessoa;
                InscricaoEmpresa = long.Parse(empresaCedente.CPF_CNPJ.JustNumbers());
                Convenio = empresaCedente.Convenio;
                NumeroAgencia = empresaCedente.NumeroAgencia;
                NomeEmpresa = empresaCedente.Nome;
                DVAgencia = empresaCedente.DVAgencia;
                NumeroConta = empresaCedente.NumeroConta;
                DVConta = empresaCedente.DVConta.Substring(0, 1);

                if (empresaCedente.DVConta.Length >= 2)
                    DVAgenciaConta = empresaCedente.DVConta.Substring(1, 1);
            }
        }

        [CampoCNAB(9, 9)]
        public string CNAB1 { get; set; }

        [CampoCNAB(18, 1)]
        public TipoInscricaoCPFCNPJEnum TipoInscricaoCpfcnpj { get; set; }

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

        [CampoCNAB(103, 30)]
        public string NomeBanco { get; set; }

        [CampoCNAB(133, 10)]
        public string CNAB2 { get; set; }

        [CampoCNAB(143, 1)]
        public TipoArquivoEnum TipoRemessaRetorno { get; set; }

        [CampoCNAB(144, 8, "ddMMyyyy")]
        public DateTime DataGeracao { get; set; }

        [CampoCNAB(152, 6, "HHmmss")]
        public DateTime HoraGeracao { get; set; }

        [CampoCNAB(158, 6)]
        public int NumeroSequencialArquivo { get; set; }

        [CampoCNAB(164, 3)]
        public int VersaoArquivo { get; set; }

        [CampoCNAB(167, 5)]
        public int DensidadeArquivo { get; set; }

        [CampoCNAB(172, 20)]
        public string ReservadorBanco { get; set; }

        [CampoCNAB(192, 20)]
        public string ReservadorEmpresa { get; set; }

        [CampoCNAB(212, 29)]
        public string CNAB3 { get; set; }
    }
}

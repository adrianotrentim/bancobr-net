using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Febraban
{
    public class HeaderArquivo : RegistroBase
    {
        public HeaderArquivo(Common.Instances.Banco banco, Correntista correntista, int numeroRemessa)
            : base(banco)
        {
            LoteServico = 0;
            TipoRegistro = 0;
            NumeroSequencialArquivo = numeroRemessa;
            DataGeracao = DateTime.Now;
            HoraGeracao = DateTime.Now;
            TipoRemessaRetorno = TipoArquivoEnum.Remessa;
            VersaoArquivo = banco.VersaoArquivo;
            NomeBanco = banco.Nome;
            DensidadeArquivo = 6250;

            if (correntista != null)
            {
                TipoInscricaoCpfcnpj = correntista.TipoPessoa;
                InscricaoEmpresa = long.Parse(correntista.CPF_CNPJ.JustNumbers());
                Convenio = correntista.Convenio;
                NumeroAgencia = correntista.NumeroAgencia;
                NomeEmpresa = correntista.Nome;
                DVAgencia = correntista.DVAgencia;
                NumeroConta = correntista.NumeroConta;
                DVConta = correntista.DVConta.Substring(0, 1);

                if (correntista.DVConta.Length >= 2)
                    DVAgenciaConta = correntista.DVConta.Substring(1, 1);
            }
        }

        [CampoCNAB(9, 9)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(18, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoCpfcnpj { get; set; }

        [CampoCNAB(19, 14)]
        public virtual long InscricaoEmpresa { get; set; }

        [CampoCNAB(33, 20)]
        public virtual string Convenio { get; set; }

        [CampoCNAB(53, 5)]
        public virtual int NumeroAgencia { get; set; }

        [CampoCNAB(58, 1)]
        public virtual string DVAgencia { get; set; }

        [CampoCNAB(59, 12)]
        public virtual int NumeroConta { get; set; }

        [CampoCNAB(71, 1)]
        public virtual string DVConta { get; set; }

        [CampoCNAB(72, 1)]
        public virtual string DVAgenciaConta { get; set; }

        [CampoCNAB(73, 30)]
        public virtual string NomeEmpresa { get; set; }

        [CampoCNAB(103, 30)]
        public virtual string NomeBanco { get; set; }

        [CampoCNAB(133, 10)]
        public virtual string CNAB2 { get; set; }

        [CampoCNAB(143, 1)]
        public virtual TipoArquivoEnum TipoRemessaRetorno { get; set; }

        [CampoCNAB(144, 8, "ddMMyyyy")]
        public virtual DateTime DataGeracao { get; set; }

        [CampoCNAB(152, 6, "HHmmss")]
        public virtual DateTime HoraGeracao { get; set; }

        [CampoCNAB(158, 6)]
        public virtual int NumeroSequencialArquivo { get; set; }

        [CampoCNAB(164, 3)]
        public virtual int VersaoArquivo { get; set; }

        [CampoCNAB(167, 5)]
        public virtual int DensidadeArquivo { get; set; }

        [CampoCNAB(172, 20)]
        public virtual string ReservadoBanco { get; set; }

        [CampoCNAB(192, 20)]
        public virtual string ReservadoEmpresa { get; set; }

        [CampoCNAB(212, 29)]
        public virtual string CNAB3 { get; set; }
    }
}

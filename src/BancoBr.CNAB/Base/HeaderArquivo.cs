using System;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Base
{
    public class HeaderArquivo : Registro
    {
        public HeaderArquivo(Common.Instances.Banco banco)
            : base(banco)
        {
            LoteServico = 0;
            TipoRegistro = 0;
            VersaoArquivo = 103;
        }

        [CNAB(4, 9)]
        public string CNAB1 { get; set; }

        [CNAB(5, 1)]
        public TipoInscricaoEmpresaEnum TipoInscricaoEmpresa { get; set; }

        [CNAB(6, 14)]
        public int InscricaoEmpresa { get; set; }

        [CNAB(7, 20)]
        public string Convenio { get; set; }

        [CNAB(8, 5)]
        public int NumeroAgencia { get; set; }

        [CNAB(9, 1)]
        public int DVAgencia { get; set; }

        [CNAB(10, 12)]
        public int NumeroConta { get; set; }

        [CNAB(11, 1)]
        public string DVConta { get; set; }

        [CNAB(12, 1)]
        public string DVAgenciaConta { get; set; }

        [CNAB(13, 30)]
        public string NomeEmpresa { get; set; }

        [CNAB(14, 30)]
        public string NomeBanco { get; set; }

        [CNAB(15, 10)]
        public string CNAB2 { get; set; }

        [CNAB(16, 1)]
        public TipoArquivoEnum CodigoRemessaRetorno { get; set; }

        [CNAB(17, 8)]
        public DateTime DataGeracao { get; set; }

        [CNAB(18, 6)]
        public DateTime HoraGeracao { get; set; }

        [CNAB(19, 6)]
        public int NumeroSequencialArquivo { get; set; }

        [CNAB(20, 3)]
        public int VersaoArquivo { get; set; }

        [CNAB(21, 5)]
        public int DensidadeArquivo { get; set; }

        [CNAB(22, 20)]
        public string ReservadorBanco { get; set; }

        [CNAB(23, 20)]
        public string ReservadorEmpresa { get; set; }

        [CNAB(24, 29)]
        public string CNAB3 { get; set; }
    }
}

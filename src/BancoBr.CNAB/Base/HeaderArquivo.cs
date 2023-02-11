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

        [CampoCNAB(4, 9)]
        public string CNAB1 { get; set; }

        [CampoCNAB(5, 1)]
        public TipoInscricaoEmpresaEnum TipoInscricaoEmpresa { get; set; }

        [CampoCNAB(6, 14)]
        public int InscricaoEmpresa { get; set; }

        [CampoCNAB(7, 20)]
        public string Convenio { get; set; }

        [CampoCNAB(8, 5)]
        public int NumeroAgencia { get; set; }

        [CampoCNAB(9, 1)]
        public int DVAgencia { get; set; }

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

        [CampoCNAB(17, 8)]
        public DateTime DataGeracao { get; set; }

        [CampoCNAB(18, 6)]
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

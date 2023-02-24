using System;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class SegmentoA : RegistroDetalheBase
    {
        public SegmentoA(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "A";
        }

        [CampoCNAB(6, 1)]
        public TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(7, 2)]
        public CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }

        [CampoCNAB(8, 3)]
        public int CamaraCentralizadora { get; set; }

        [CampoCNAB(9, 3)]
        public int BancoFavorecido { get; set; }

        [CampoCNAB(10, 5)]
        public int AgenciaFavorecido { get; set; }

        [CampoCNAB(11, 1)]
        public string DVAgenciaFavorecido { get; set; }

        [CampoCNAB(12, 12)]
        public int ContaFavorecido { get; set; }

        [CampoCNAB(13, 1)]
        public string DVContaFavorecido { get; set; }

        [CampoCNAB(14, 1)]
        public string DVAgenciaContaFavorecido { get; set; }

        [CampoCNAB(15, 30)]
        public string NomeFavorecido { get; set; }

        [CampoCNAB(16, 20)]
        public string NumeroDocumentoEmpresa { get; set; }

        [CampoCNAB(17, 8)]
        public DateTime DataPagamento { get; set; }

        [CampoCNAB(18, 3)]
        public string TipoMoeda { get; set; }
        
        [CampoCNAB(19, 15)]
        public decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(20, 15)]
        public decimal ValorPagamento { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(21, 20)]
        public string NumeroDocumentoBanco { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(22, 8)]
        public DateTime DataRealPagamento { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(23, 15)]
        public decimal ValorRealPagamento { get; set; }

        [CampoCNAB(24, 40)]
        public string Informacao2 { get; set; }

        /// <summary>
        /// Verificar Nota P005 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(25, 2)]
        public string CodigoFinalidadeDOC{ get; set; }

        /// <summary>
        /// Verificar Nota P011 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(26, 5)]
        public string CodigoFinalidadeTED { get; set; }

        [CampoCNAB(27, 2)]
        public string CodigoFinalidadeComplementar { get; set; }

        [CampoCNAB(28, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(29, 1)]
        public AvisoFavorecidoEnum AvisoFavorecido { get; set; }

        [CampoCNAB(30, 10)]
        public string Ocorrencias { get; set; }
    }
}
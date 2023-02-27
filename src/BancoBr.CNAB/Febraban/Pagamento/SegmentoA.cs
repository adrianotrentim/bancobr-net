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

        [CampoCNAB(15, 1)]
        public TipoMovimentoEnum TipoMovimento { get; set; }

        [CampoCNAB(16, 2)]
        public CodigoInstrucaoMovimentoEnum CodigoInstrucaoMovimento { get; set; }

        [CampoCNAB(18, 3)]
        public int CamaraCentralizadora { get; set; }

        [CampoCNAB(21, 3)]
        public int BancoFavorecido { get; set; }

        [CampoCNAB(24, 5)]
        public int AgenciaFavorecido { get; set; }

        [CampoCNAB(29, 1)]
        public string DVAgenciaFavorecido { get; set; }

        [CampoCNAB(30, 12)]
        public int ContaFavorecido { get; set; }

        [CampoCNAB(42, 1)]
        public string DVContaFavorecido { get; set; }

        [CampoCNAB(43, 1)]
        public string DVAgenciaContaFavorecido { get; set; }

        [CampoCNAB(44, 30)]
        public string NomeFavorecido { get; set; }

        [CampoCNAB(74, 20)]
        public string NumeroDocumentoEmpresa { get; set; }

        [CampoCNAB(94, 8)]
        public DateTime DataPagamento { get; set; }

        [CampoCNAB(102, 3)]
        public string TipoMoeda { get; set; }
        
        [CampoCNAB(105, 15)]
        public decimal QuantidadeMoeda { get; set; }

        [CampoCNAB(120, 15)]
        public decimal ValorPagamento { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(135, 20)]
        public string NumeroDocumentoBanco { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(155, 8)]
        public DateTime DataRealPagamento { get; set; }

        /// <summary>
        /// Somente no arquivo de Retorno
        /// </summary>
        [CampoCNAB(163, 15)]
        public decimal ValorRealPagamento { get; set; }

        [CampoCNAB(178, 40)]
        public string Informacao2 { get; set; }

        /// <summary>
        /// Verificar Nota P005 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(218, 2)]
        public string CodigoFinalidadeDOC{ get; set; }

        /// <summary>
        /// Verificar Nota P011 - CNAB240 FEBRABAN
        /// </summary>
        [CampoCNAB(220, 5)]
        public string CodigoFinalidadeTED { get; set; }

        [CampoCNAB(225, 2)]
        public string CodigoFinalidadeComplementar { get; set; }

        [CampoCNAB(227, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(230, 1)]
        public AvisoFavorecidoEnum AvisoFavorecido { get; set; }

        [CampoCNAB(231, 10)]
        public string Ocorrencias { get; set; }
    }
}
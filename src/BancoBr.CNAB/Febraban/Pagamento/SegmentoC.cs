using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class SegmentoC : RegistroDetalheBase
    {
        public SegmentoC(Common.Instances.Banco banco)
            : base(banco)
        {
            TipoRegistro = 3;
            CodigoSegmento = "C";
        }

        [CampoCNAB(6, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(7, 15)]
        public decimal ValorIR { get; set; }

        [CampoCNAB(8, 15)]
        public decimal ValorISS { get; set; }

        [CampoCNAB(9, 15)]
        public decimal ValorIOF { get; set; }

        [CampoCNAB(10, 15)]
        public decimal ValorOutrasDeducoes { get; set; }

        [CampoCNAB(11, 15)]
        public decimal ValorOutrosAcrescimos { get; set; }

        [CampoCNAB(12, 5)]
        public int AgenciaSubistituta { get; set; }

        [CampoCNAB(13, 1)]
        public string DVAgenciaSubistituta { get; set; }

        [CampoCNAB(14, 12)]
        public int ContaSubistituta { get; set; }

        [CampoCNAB(15, 1)]
        public string DVContaSubistituta { get; set; }

        [CampoCNAB(16, 1)]
        public string DVAgenciaContaSubistituta { get; set; }

        [CampoCNAB(17, 15)]
        public decimal ValorINSS { get; set; }

        [CampoCNAB(18, 20)]
        public int ContaPagamentoCreditada { get; set; }

        [CampoCNAB(19, 93)]
        public string CNAB2 { get; set; }
    }
}
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

        [CampoCNAB(15, 3)]
        public string CNAB1 { get; set; }

        [CampoCNAB(18, 15)]
        public decimal ValorIR { get; set; }

        [CampoCNAB(33, 15)]
        public decimal ValorISS { get; set; }

        [CampoCNAB(48, 15)]
        public decimal ValorIOF { get; set; }

        [CampoCNAB(63, 15)]
        public decimal ValorOutrasDeducoes { get; set; }

        [CampoCNAB(78, 15)]
        public decimal ValorOutrosAcrescimos { get; set; }

        [CampoCNAB(93, 5)]
        public int AgenciaSubistituta { get; set; }

        [CampoCNAB(98, 1)]
        public string DVAgenciaSubistituta { get; set; }

        [CampoCNAB(99, 12)]
        public int ContaSubistituta { get; set; }

        [CampoCNAB(111, 1)]
        public string DVContaSubistituta { get; set; }

        [CampoCNAB(112, 1)]
        public string DVAgenciaContaSubistituta { get; set; }

        [CampoCNAB(113, 15)]
        public decimal ValorINSS { get; set; }

        [CampoCNAB(128, 20)]
        public int ContaPagamentoCreditada { get; set; }

        [CampoCNAB(148, 93)]
        public string CNAB2 { get; set; }
    }
}
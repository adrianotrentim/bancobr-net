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
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(18, 15)]
        public virtual decimal ValorIR { get; set; }

        [CampoCNAB(33, 15)]
        public virtual decimal ValorISS { get; set; }

        [CampoCNAB(48, 15)]
        public virtual decimal ValorIOF { get; set; }

        [CampoCNAB(63, 15)]
        public virtual decimal ValorOutrasDeducoes { get; set; }

        [CampoCNAB(78, 15)]
        public virtual decimal ValorOutrosAcrescimos { get; set; }

        [CampoCNAB(93, 5)]
        public virtual int AgenciaSubistituta { get; set; }

        [CampoCNAB(98, 1)]
        public virtual string DVAgenciaSubistituta { get; set; }

        [CampoCNAB(99, 12)]
        public virtual int ContaSubistituta { get; set; }

        [CampoCNAB(111, 1)]
        public virtual string DVContaSubistituta { get; set; }

        [CampoCNAB(112, 1)]
        public virtual string DVAgenciaContaSubistituta { get; set; }

        [CampoCNAB(113, 15)]
        public virtual decimal ValorINSS { get; set; }

        [CampoCNAB(128, 20)]
        public virtual int ContaPagamentoCreditada { get; set; }

        [CampoCNAB(148, 93)]
        public virtual string CNAB2 { get; set; }
    }
}
using System.Linq;
using System.Runtime.Serialization;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Febraban
{
    public class TrailerLote : TrailerLoteBase
    {
        public TrailerLote(Lote lote)
            : base(lote)
        {
        }

        [CampoCNAB(24, 18)]
        public virtual decimal Valor =>
            Lote.Detalhe.Where(t => t is SegmentoA).Sum(t => ((SegmentoA)t).ValorPagamento) +
            Lote.Detalhe.Where(t => t is SegmentoJ).Sum(t => ((SegmentoJ)t).ValorPagamento) +
            Lote.Detalhe.Where(t => t is SegmentoO).Sum(t => ((SegmentoO)t).ValorPagamento);

        [CampoCNAB(42, 18)]
        public virtual decimal QuantidadeMoeda =>
            Lote.Detalhe.Where(t => t is SegmentoA).Sum(t => ((SegmentoA)t).QuantidadeMoeda) +
            Lote.Detalhe.Where(t => t is SegmentoJ).Sum(t => ((SegmentoJ)t).QuantidadeMoeda);

        [CampoCNAB(60, 6)]
        public virtual int NumeroAvisoDebito { get; set; }

        [CampoCNAB(66, 165)]
        public virtual string CNAB2 { get; set; }

        [CampoCNAB(231, 10)]
        public virtual string Ocorrencias { get; set; }
    }
}
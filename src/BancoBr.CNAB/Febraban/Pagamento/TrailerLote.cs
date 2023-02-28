using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Febraban.Pagamento
{
    public class TrailerLote : TrailerLoteBase
    {
        public TrailerLote(Lote lote)
            : base(lote)
        {
        }

        [CampoCNAB(24, 18)] 
        public virtual decimal Valor => Lote.Registros.Where(t => t.CodigoSegmento == "A").Sum(t => ((SegmentoA)t).ValorPagamento);

        [CampoCNAB(42, 18)]
        public virtual decimal QuantidadeMoeda => Lote.Registros.Where(t => t.TipoRegistro == 3 && t.CodigoSegmento == "A").Sum(t => ((SegmentoA)t).QuantidadeMoeda);

        [CampoCNAB(60, 6)]
        public virtual int NumeroAvisoDebito { get; set; }

        [CampoCNAB(66, 165)]
        public virtual string CNAB2 { get; set; }

        [CampoCNAB(231, 10)]
        public virtual string Ocorrencias { get; set; }
    }
}
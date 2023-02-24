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

        [CampoCNAB(6, 18)] 
        public decimal Valor => Lote.Registros.Where(t => t.CodigoSegmento == "A").Sum(t => ((SegmentoA)t).ValorPagamento);

        [CampoCNAB(7, 18)]
        public decimal QuantidadeMoeda => Lote.Registros.Where(t => t.TipoRegistro == 3 && t.CodigoSegmento == "A").Sum(t => ((SegmentoA)t).QuantidadeMoeda);

        [CampoCNAB(8, 6)]
        public int NumeroAvisoDebito { get; set; }

        [CampoCNAB(9, 165)]
        public string CNAB2 { get; set; }

        [CampoCNAB(10, 10)]
        public string Ocorrencias { get; set; }
    }
}
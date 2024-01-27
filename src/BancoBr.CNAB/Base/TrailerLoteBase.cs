using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Base
{
    public class TrailerLoteBase : RegistroBase
    {
        public Lote Lote { get; }

        public TrailerLoteBase(Lote lote)
            : base(lote.Header.Banco)
        {
            TipoRegistro = 5;

            Lote = lote;
        }

        [CampoCNAB(4, 4)] 
        public override int LoteServico => Lote.Header.LoteServico;

        [CampoCNAB(9, 9)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(18, 6)] 
        public virtual int QuantidadeRegistros => Lote.Detalhe.Count;
    }
}
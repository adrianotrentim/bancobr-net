using BancoBr.CNAB.Base;
using BancoBr.Common.Instances;
using System.Collections.Generic;

namespace BancoBr.CNAB.Itau
{
    public class Banco : Base.Banco
    {
        public Banco()
            : base(341, "Banco Itaú", 80)
        {
        }

        public override RegistroBase NovoHeaderArquivo(Pessoa empresaCedente, int numeroRemessa) => new HeaderArquivo(this, empresaCedente, numeroRemessa);
        public override RegistroBase NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Bloco de Pagamentos .::

        //public override HeaderLoteBase NovoHeaderLote() => new HeaderLote(this);
        //public override RegistroDetalheBase NovoSegmentoA() => new SegmentoA(this);
        //public override RegistroDetalheBase NovoSegmentoB() => new SegmentoB(this);

        public override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Common.Instances.Pagamento pagamento) => null;
        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Pagamento pagamento) => null;
        public override RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Common.Instances.Pagamento pagamento) => null;

        #endregion
    }
}

using System.Collections.Generic;
using BancoBr.CNAB.Febraban.PagamentoTitulo;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public abstract class Banco : Common.Instances.Banco
    {
        protected Banco(int codigo, string nome) 
            : base(codigo, nome)
        {
        }

        #region ::. Bloco de Pagamentos .::

        protected internal virtual Lote NovoLotePagamento()
        {
            var lote = new Lote();
            lote.Header = new Febraban.PagamentoTitulo.HeaderLote(this);

            return lote;
        }

        protected internal virtual List<Registro> NovoPagamento(Titulo titulo)
        {
            var registros = new List<Registro>();
            var segmentoA = new SegmentoJ(this);
            //Preenche o segmentoA
            //segmentoA.CampoQualquer = titulo.CampoQualquer

            //Adiciona outros registros
            //var segmentoB = new Febraban.Pagamento.SegmentoB(this);

            registros.Add(segmentoA);

            return registros;
        }

        #endregion
    }
}

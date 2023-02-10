using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public class Banco : Common.Instances.Banco
    {
        public Banco(int codigo, string nome) 
            : base(codigo, nome)
        {
        }

        #region ::. Bloco de Pagamentos .::

        protected internal virtual Lote GetLotePagamento()
        {
            var lote = new Lote();
            lote.Header = new Febraban.Pagamento.HeaderLote(this);

            return lote;
        }

        protected internal virtual List<Registro> GetPagamento(Titulo titulo)
        {
            var registros = new List<Registro>();
            var segmentoA = new Febraban.Pagamento.SegmentoA(this);
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

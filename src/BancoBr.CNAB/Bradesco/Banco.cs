using System.Collections.Generic;
using System.Runtime.InteropServices;
using BancoBr.CNAB.Base;
using BancoBr.Common.Instances;
using BancoBr.Common.Interfaces;

namespace BancoBr.CNAB.Bradesco
{
    public class Banco : Base.Banco
    {
        public Banco()
            : base(237, "Banco Bradesco SA")
        {
        }

        #region ::. Bloco de Pagamentos .::

        ////SOMENTE EXEMPLO - Se o lote de pagamento for diferente do febraban , retorna a instancia do lote próprio do bradesco
        //protected internal override Lote GetLotePagamento()
        //{
        //    var lote = new Lote();
        //    lote.Header = new Bradesco.Pagamento.HeaderLote(this);

        //    return lote;
        //}

        ////SOMENTE EXEMPLO - Se o pagamento for diferente do febraban , retorna a instancia do pagamento próprio do bradesco
        //protected internal override List<Registro> GetPagamento(Titulo titulo)
        //{
        //    var registros = new List<Registro>();
        //    var segmentoA = new Bradesco.Pagamento.SegmentoA(this);

        //    //Adiciona outros registros

        //    registros.Add(segmentoA);

        //    return registros;
        //}

        #endregion


    }
}

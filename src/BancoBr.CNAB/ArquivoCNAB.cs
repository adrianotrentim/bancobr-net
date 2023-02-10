using System;
using System.Collections.Generic;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;

namespace BancoBr.CNAB
{
    public class ArquivoCNAB
    {
        public ArquivoCNAB(Bancos banco)
        {
            switch (banco)
            {
                case Bancos.BradescoSA:
                    Banco = new Bradesco.Banco();
                    break;
                case Bancos.Santander:
                    Banco = new Santander.Banco();
                    break;
                default:
                    throw new Exception("Banco não implementado!");
            }

            Header = new HeaderArquivo(Banco);
            Lotes = new List<Lote>();
        }

        public Banco Banco { get; }
        public HeaderArquivo Header { get; set; }
        public List<Lote> Lotes { get; set; }
        public TrailerArquivo Trailer => new TrailerArquivo(this, Lotes);

        #region ::. Blocos de Pagamento .::

        public Lote NovoLotePagamento()
        {
            var lote = Banco.GetLotePagamento();
            Lotes.Add(lote);

            return lote;
        }

        #endregion
    }
}

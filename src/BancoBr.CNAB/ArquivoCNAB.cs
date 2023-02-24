using System;
using System.Collections.Generic;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Banco = BancoBr.CNAB.Base.Banco;

namespace BancoBr.CNAB
{
    public class ArquivoCNAB
    {
        private int _numeroLote = 1;

        public ArquivoCNAB(BancoEnum banco, Pessoa empresaCedente, int numeroRemessa)
        {
            switch (banco)
            {
                case BancoEnum.BradescoSA:
                    Banco = new Bradesco.Banco();
                    break;
                case BancoEnum.Itau:
                    Banco = new Itau.Banco();
                    break;
                default:
                    throw new Exception("Banco não implementado!");
            }

            EmpresaCedente = empresaCedente;
            Header = new HeaderArquivo(Banco, empresaCedente, numeroRemessa);

            Lotes = new List<Lote>();
        }

        public Banco Banco { get; }
        public Pessoa EmpresaCedente { get; }
        public HeaderArquivo Header { get; set; }
        public List<Lote> Lotes { get; set; }
        public TrailerArquivo Trailer => new TrailerArquivo(this, Lotes);

        #region ::. Blocos de Pagamento .::

        public Lote NovoLotePagamento(TipoServicoEnum tipoServico, TipoLancamentoEnum tipoLancamento, FormaPagamentoEnum formaPagamento)
        {
            var lote = Banco.NovoLotePagamento(EmpresaCedente, tipoServico, formaPagamento, tipoLancamento);

            lote.Header.LoteServico = _numeroLote;

            Lotes.Add(lote);

            _numeroLote = Lotes.Count + 1;

            return lote;
        }

        #endregion
    }
}

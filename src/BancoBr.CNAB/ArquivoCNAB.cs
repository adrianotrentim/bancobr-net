﻿using System;
using System.Collections.Generic;
using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Banco = BancoBr.CNAB.Base.Banco;

namespace BancoBr.CNAB
{
    public class ArquivoCNAB
    {
        private int _numeroLote = 1;

        /// <summary>
        /// Cria uma nova instâcia de CNAB240 utilizada somente para importação de retorno
        /// </summary>
        /// <param name="Banco"></param>
        /// <param name="Correntista"></param>
        /// <exception cref="Exception"></exception>
        public ArquivoCNAB(BancoEnum banco, Correntista correntista)
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

            Correntista = correntista;
            Header = Banco.NovoHeaderArquivo(correntista, 0);
            Lotes = new List<Lote>();
        }

        /// <summary>
        /// Cria uma nova instâcia de CNAB240 com seus respectivos lotes e movimentos
        /// </summary>
        /// <param name="Banco"></param>
        /// <param name="Correntista"></param>
        /// <exception cref="Exception"></exception>
        public ArquivoCNAB(BancoEnum banco, Correntista correntista, int numeroRemessa, LocalDebitoEnum localDebito, TipoServicoEnum tipoServico, List<Movimento> movimentos)
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

            Correntista = correntista;
            Header = Banco.NovoHeaderArquivo(correntista, numeroRemessa);

            GerarLotes(tipoServico, localDebito, movimentos);
        }

        public Banco Banco { get; }
        public Correntista Correntista { get; }
        public RegistroBase Header { get; set; }
        public List<Lote> Lotes { get; set; }
        public RegistroBase Trailer => Banco.NovoTrailerArquivo(this, Lotes);

        #region ::. Blocos de Movimento .::

        private void GerarLotes(TipoServicoEnum tipoServico, LocalDebitoEnum localDebito, List<Movimento> movimentos)
        {
            Lotes = new List<Lote>();

            foreach (var item in movimentos.GroupBy(t => t.TipoLancamento).ToList())
            {
                var lote = Banco.NovoLote(Correntista, tipoServico, item.Key, localDebito);

                lote.Header.LoteServico = _numeroLote;

                Lotes.Add(lote);

                foreach (var mov in item)
                    lote.AdicionarMovimento(mov);

                _numeroLote = Lotes.Count + 1;
            }
        }

        #endregion
    }
}

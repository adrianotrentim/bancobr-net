﻿using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        public ArquivoCNAB(BancoEnum banco)
        {
            switch (banco)
            {
                case BancoEnum.BradescoSA:
                    Banco = new Bradesco.Banco(null);
                    break;
                case BancoEnum.Itau:
                    Banco = new Itau.Banco(null);
                    break;
                case BancoEnum.CEF: 
                    Banco = new CEF.Banco(null);
                    break;
                case BancoEnum.Sicredi:
                    Banco = new Sicredi.Banco(null);
                    break;
                case BancoEnum.Inter:
                    Banco = new Inter.Banco(null);
                    break;
                case BancoEnum.Santander:
                    Banco = new Santander.Banco(null);
                    break;
                default:
                    throw new Exception("Banco não implementado!");
            }

            Header  = Banco.NovoHeaderArquivo(0, null);

            Lotes = new List<Lote>();
            Movimentos = new List<Movimento>();
        }

        /// <summary>
        /// Cria uma nova instâcia de CNAB240 com seus respectivos lotes e movimentos
        /// </summary>
        /// <param name="Banco"></param>
        /// <param name="Correntista"></param>
        /// <exception cref="Exception"></exception>
        public ArquivoCNAB(BancoEnum banco, Correntista correntista, int numeroRemessa, LocalDebitoEnum localDebito, TipoServicoEnum tipoServico, List<Movimento> movimentos)
        {
            if (movimentos.Any(t => t.TipoLancamento == 0))
                throw new Exception($"O(s) movimento(s) de número(s) {string.Join(", ", movimentos.Where(t => t.TipoLancamento == 0).Select(t => t.NumeroDocumento))} não possui(em) tipo de lançamento!");

            Movimentos = movimentos;

            switch (banco)
            {
                case BancoEnum.BradescoSA:
                    Banco = new Bradesco.Banco(correntista);
                    break;
                case BancoEnum.Itau:
                    Banco = new Itau.Banco(correntista);
                    break;
                case BancoEnum.CEF:
                    Banco = new CEF.Banco(correntista);
                    break;
                case BancoEnum.Sicredi:
                    Banco = new Sicredi.Banco(correntista);
                    break;
                case BancoEnum.Inter:
                    Banco = new Inter.Banco(correntista);
                    break;
                case BancoEnum.Santander:
                    Banco = new Santander.Banco(correntista);
                    break;
                default:
                    throw new Exception("Banco não implementado!");
            }

            Header = Banco.CriarHeaderArquivo(numeroRemessa, movimentos);

            GerarLotes(tipoServico, localDebito, movimentos);
        }

        public Banco Banco { get; }
        public List<Movimento> Movimentos { get; }
        public string Arquivo { get; set; }

        public HeaderArquivo Header { get; set; }
        public List<Lote> Lotes { get; set; }
        public TrailerArquivo Trailer => Banco.NovoTrailerArquivo(this, Lotes);

        #region ::. Blocos de Movimento .::

        private void GerarLotes(TipoServicoEnum tipoServico, LocalDebitoEnum localDebito, List<Movimento> movimentos)
        {
            Lotes = new List<Lote>();

            foreach (var item in movimentos.GroupBy(t => t.TipoLancamento).ToList())
            {
                var lote = Banco.NovoLote(tipoServico, item.Key, localDebito);

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

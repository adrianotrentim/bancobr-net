using System;
using System.Collections.Generic;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public abstract class Banco : Common.Instances.Banco
    {
        private Pessoa _empresaCedente;
        private TipoServicoEnum _tipoServico;
        private TipoLancamentoEnum _tipoLancamento;
        private LocalDebitoEnum _localDebito;

        protected Banco(int codigo, string nome, int versaoArquivo)
            : base(codigo, nome, versaoArquivo)
        {
        }

        public virtual RegistroBase NovoHeaderArquivo(Pessoa empresaCedente, int numeroRemessa) => new HeaderArquivo(this, empresaCedente, numeroRemessa);
        public virtual RegistroBase NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Métodos Públicos .::

        public Lote NovoLote(Pessoa empresaCedente, TipoServicoEnum tipoServico, TipoLancamentoEnum tipoLancamento, LocalDebitoEnum localDebito)
        {
            _empresaCedente = empresaCedente;
            _tipoServico = tipoServico;
            _tipoLancamento = tipoLancamento;
            _localDebito = localDebito;

            if (tipoLancamento == TipoLancamentoEnum.CartaoSalario && tipoServico != TipoServicoEnum.PagamentoSalarios)
                throw new InvalidOperationException("A forma de movimento Cartão Salário (4), só é permitida para o Tipo de Serviço Movimento de Salários (30)");

            var lote = new Lote
            {
                Header = PreencheHeaderLoteBase()
            };

            lote.Trailer = PreencheTrailerLoteBase(lote);

            return lote;
        }

        public List<RegistroDetalheBase> NovoMovimento(Movimento movimento, int numeroLote, int numeroRegistro)
        {
            var registros = new List<RegistroDetalheBase>();

            var segmentoA = PreencheSegmentoABase(movimento, numeroLote);
            var segmentoB = PreencheSegmentoBBase(movimento, numeroLote);
            var segmentoC = PreencheSegmentoCBase(movimento, numeroLote);
            var segmentoJ = PreencheSegmentoJBase(movimento, numeroLote);
            var segmentoJ52 = PreencheSegmentoJ52Base(movimento, numeroLote);

            if (segmentoA != null)
            {
                numeroRegistro++;
                segmentoA.NumeroRegistro = numeroRegistro;
                registros.Add(segmentoA);
            }

            if (segmentoB != null)
            {
                numeroRegistro++;
                segmentoB.NumeroRegistro = numeroRegistro;
                registros.Add(segmentoB);
            }

            if (segmentoC != null)
            {
                numeroRegistro++;
                segmentoC.NumeroRegistro = numeroRegistro;
                registros.Add(segmentoC);
            }

            if (segmentoJ != null)
            {
                numeroRegistro++;
                segmentoJ.NumeroRegistro = numeroRegistro;
                registros.Add(segmentoJ);
            }

            if (segmentoJ52 != null)
            {
                numeroRegistro++;
                segmentoJ52.NumeroRegistro = numeroRegistro;
                registros.Add(segmentoJ52);
            }

            return registros;
        }

        #endregion

        #region ::. Métodos Privados .::

        private HeaderLoteBase PreencheHeaderLoteBase()
        {
            var headerLote = NovoHeaderLote(_tipoLancamento);

            if (headerLote is HeaderLote_PagamentoTransferencia)
            {
                ((HeaderLote_PagamentoTransferencia)headerLote).Servico = _tipoServico;
                ((HeaderLote_PagamentoTransferencia)headerLote).TipoLancamento = _tipoLancamento;
                ((HeaderLote_PagamentoTransferencia)headerLote).TipoInscricaoEmpresa = _empresaCedente.TipoPessoa;
                ((HeaderLote_PagamentoTransferencia)headerLote).InscricaoEmpresa = long.Parse(_empresaCedente.CPF_CNPJ.JustNumbers());
                ((HeaderLote_PagamentoTransferencia)headerLote).NumeroAgencia = _empresaCedente.NumeroAgencia;
                ((HeaderLote_PagamentoTransferencia)headerLote).DVAgencia = _empresaCedente.DVAgencia;
                ((HeaderLote_PagamentoTransferencia)headerLote).NumeroConta = _empresaCedente.NumeroConta;
                ((HeaderLote_PagamentoTransferencia)headerLote).DVConta = _empresaCedente.DVConta;
                ((HeaderLote_PagamentoTransferencia)headerLote).Convenio = _empresaCedente.Convenio;

                if (_empresaCedente.DVConta.Length >= 2)
                {
                    ((HeaderLote_PagamentoTransferencia)headerLote).DVConta = _empresaCedente.DVConta.Substring(0, 1);
                    ((HeaderLote_PagamentoTransferencia)headerLote).DVAgenciaConta = _empresaCedente.DVConta.Substring(1, 1);
                }

                ((HeaderLote_PagamentoTransferencia)headerLote).NomeEmpresa = _empresaCedente.Nome;
                ((HeaderLote_PagamentoTransferencia)headerLote).EnderecoEmpresa = _empresaCedente.Endereco;
                ((HeaderLote_PagamentoTransferencia)headerLote).NumeroEnderecoEmpresa = _empresaCedente.NumeroEndereco;
                ((HeaderLote_PagamentoTransferencia)headerLote).ComplementoEnderecoEmpresa = _empresaCedente.ComplementoEndereco;
                ((HeaderLote_PagamentoTransferencia)headerLote).CidadeEmpresa = _empresaCedente.Cidade;
                ((HeaderLote_PagamentoTransferencia)headerLote).CEPEmpresa = _empresaCedente.CEP;
                ((HeaderLote_PagamentoTransferencia)headerLote).UFEmpresa = _empresaCedente.UF;
                ((HeaderLote_PagamentoTransferencia)headerLote).LocalDebito = _localDebito;
            }
            else if (headerLote is HeaderLote_PagamentoTitulo)
            {
                ((HeaderLote_PagamentoTitulo)headerLote).Servico = _tipoServico;
                ((HeaderLote_PagamentoTitulo)headerLote).TipoLancamento = _tipoLancamento;
                ((HeaderLote_PagamentoTitulo)headerLote).TipoInscricaoEmpresa = _empresaCedente.TipoPessoa;
                ((HeaderLote_PagamentoTitulo)headerLote).InscricaoEmpresa = long.Parse(_empresaCedente.CPF_CNPJ.JustNumbers());
                ((HeaderLote_PagamentoTitulo)headerLote).NumeroAgencia = _empresaCedente.NumeroAgencia;
                ((HeaderLote_PagamentoTitulo)headerLote).DVAgencia = _empresaCedente.DVAgencia;
                ((HeaderLote_PagamentoTitulo)headerLote).NumeroConta = _empresaCedente.NumeroConta;
                ((HeaderLote_PagamentoTitulo)headerLote).DVConta = _empresaCedente.DVConta;
                ((HeaderLote_PagamentoTitulo)headerLote).Convenio = _empresaCedente.Convenio;

                if (_empresaCedente.DVConta.Length >= 2)
                {
                    ((HeaderLote_PagamentoTitulo)headerLote).DVConta = _empresaCedente.DVConta.Substring(0, 1);
                    ((HeaderLote_PagamentoTitulo)headerLote).DVAgenciaConta = _empresaCedente.DVConta.Substring(1, 1);
                }

                ((HeaderLote_PagamentoTitulo)headerLote).NomeEmpresa = _empresaCedente.Nome;
                ((HeaderLote_PagamentoTitulo)headerLote).EnderecoEmpresa = _empresaCedente.Endereco;
                ((HeaderLote_PagamentoTitulo)headerLote).NumeroEnderecoEmpresa = _empresaCedente.NumeroEndereco;
                ((HeaderLote_PagamentoTitulo)headerLote).ComplementoEnderecoEmpresa = _empresaCedente.ComplementoEndereco;
                ((HeaderLote_PagamentoTitulo)headerLote).CidadeEmpresa = _empresaCedente.Cidade;
                ((HeaderLote_PagamentoTitulo)headerLote).CEPEmpresa = _empresaCedente.CEP;
                ((HeaderLote_PagamentoTitulo)headerLote).UFEmpresa = _empresaCedente.UF;
            }

            return PreencheHeaderLote(headerLote);
        }

        private TrailerLoteBase PreencheTrailerLoteBase(Lote lote)
        {
            var trailerLote = (TrailerLote)NovoTrailerLote(lote);

            return PreencheTrailerLote(trailerLote);
        }

        private RegistroDetalheBase PreencheSegmentoABase(Movimento movimento, int numeroLote)
        {
            if (movimento.MovimentoPagamentoTransferencia == null)
                return null;

            if (
                (_tipoLancamento == TipoLancamentoEnum.TEDMesmaTitularidade ||
                _tipoLancamento == TipoLancamentoEnum.TEDOutraTitularidade)
                &&
                movimento.MovimentoPagamentoTransferencia.CodigoFinalidadeTED == FinalidadeTEDEnum.NaoAplicavel
                )
                throw new InvalidOperationException("Para a forma de movimento TED, você deve informar uma Finalidade");

            var segmento = (SegmentoA)NovoSegmentoA(_tipoLancamento);

            segmento.LoteServico = numeroLote;
            segmento.TipoMovimento = movimento.TipoMovimento;
            segmento.CodigoInstrucaoMovimento = movimento.CodigoInstrucao;

            switch (segmento.TipoMovimento)
            {
                case TipoMovimentoEnum.Inclusao:
                    if (
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado &&
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado
                        )
                        throw new Exception($"Para movimento de inclusão, favor utilizar os códigos de instrução:\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheLiberado.GetDescription()}\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.InclusaoRegistroDetalheBloqueado.GetDescription()}");
                    break;

                case TipoMovimentoEnum.Alteracao:
                    if (
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.AlteracaoPagamentoLiberadoParaBloqueio &&
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.AlteracaoPagamentoBloqueadoParaLiberacao &&
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.AlteracaoValorTitulo &&
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.AlteracaoDataPagamento &&
                        segmento.CodigoInstrucaoMovimento != CodigoInstrucaoMovimentoEnum.PagamentoDiretoFornecedor_Baixar
                    )
                        throw new Exception($"Para movimento de alteração, favor utilizar os códigos de instrução:\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.AlteracaoPagamentoLiberadoParaBloqueio.GetDescription()}\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.AlteracaoPagamentoBloqueadoParaLiberacao.GetDescription()}\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.AlteracaoValorTitulo.GetDescription()}\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.AlteracaoDataPagamento.GetDescription()}\r\n" +
                                            $"{CodigoInstrucaoMovimentoEnum.PagamentoDiretoFornecedor_Baixar.GetDescription()}");
                    break;
                case TipoMovimentoEnum.Exclusao:
                    segmento.CodigoInstrucaoMovimento = CodigoInstrucaoMovimentoEnum.ExclusaoRegistroDetalheIncluidoAnteriormente;
                    break;
            }

            switch (_tipoLancamento)
            {
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    segmento.CamaraCentralizadora = 18;

                    ((SegmentoA_Transferencia)segmento).BancoFavorecido = movimento.PessoaEmpresaDestino.Banco;
                    ((SegmentoA_Transferencia)segmento).AgenciaFavorecido = movimento.PessoaEmpresaDestino.NumeroAgencia;
                    ((SegmentoA_Transferencia)segmento).DVAgenciaFavorecido = movimento.PessoaEmpresaDestino.DVAgencia.Substring(0, 1);
                    ((SegmentoA_Transferencia)segmento).ContaFavorecido = movimento.PessoaEmpresaDestino.NumeroConta;
                    ((SegmentoA_Transferencia)segmento).DVContaFavorecido = movimento.PessoaEmpresaDestino.DVConta;

                    if (movimento.PessoaEmpresaDestino.DVConta.Length >= 2)
                    {
                        ((SegmentoA_Transferencia)segmento).DVContaFavorecido = movimento.PessoaEmpresaDestino.DVConta.Substring(0, 1);
                        ((SegmentoA_Transferencia)segmento).DVAgenciaContaFavorecido = movimento.PessoaEmpresaDestino.DVConta.Substring(1, 1);
                    }

                    break;
                case TipoLancamentoEnum.PIXTransferencia:
                case TipoLancamentoEnum.PIXQrCode:
                    segmento.CamaraCentralizadora = 9;
                    break;
            }

            segmento.NomeFavorecido = movimento.PessoaEmpresaDestino.Nome;
            segmento.NumeroDocumentoEmpresa = movimento.MovimentoPagamentoTransferencia.NumeroDocumento;
            segmento.DataPagamento = movimento.MovimentoPagamentoTransferencia.DataPagamento;
            segmento.TipoMoeda = movimento.MovimentoPagamentoTransferencia.Moeda;
            segmento.QuantidadeMoeda = movimento.MovimentoPagamentoTransferencia.QuantidadeMoeda;
            segmento.ValorPagamento = movimento.MovimentoPagamentoTransferencia.ValorPagamento;

            switch (segmento.CamaraCentralizadora)
            {
                case 18:
                    if (
                        _tipoLancamento == TipoLancamentoEnum.TEDMesmaTitularidade ||
                        _tipoLancamento == TipoLancamentoEnum.TEDOutraTitularidade ||
                        _tipoLancamento == TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco ||
                        _tipoLancamento == TipoLancamentoEnum.CreditoContaMesmoBanco
                    )
                        ((SegmentoA_Transferencia)segmento).CodigoFinalidadeTED = movimento.MovimentoPagamentoTransferencia.CodigoFinalidadeTED;
                    break;
            }

            segmento.AvisoFavorecido = movimento.AvisoAoFavorecido;

            return PreencheSegmentoA(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Movimento movimento, int numeroLote)
        {
            if (movimento.MovimentoPagamentoTransferencia == null)
                return null;

            if (
                _tipoLancamento == TipoLancamentoEnum.CreditoContaMesmoBanco ||
                _tipoLancamento == TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco ||
                _tipoLancamento == TipoLancamentoEnum.OrdemPagamento ||
                _tipoLancamento == TipoLancamentoEnum.TEDMesmaTitularidade ||
                _tipoLancamento == TipoLancamentoEnum.TEDOutraTitularidade
            )
            {
                var segmento = (SegmentoB_Transferencia)NovoSegmentoB(_tipoLancamento);

                segmento.LoteServico = numeroLote;

                segmento.TipoInscricaoFavorecido = movimento.PessoaEmpresaDestino.TipoPessoa;
                segmento.InscricaoFavorecido = long.Parse(movimento.PessoaEmpresaDestino.CPF_CNPJ.JustNumbers());
                segmento.EnderecoFavorecido = movimento.PessoaEmpresaDestino.Endereco;
                segmento.NumeroEnderecoFavorecido = movimento.PessoaEmpresaDestino.NumeroEndereco;
                segmento.ComplementoEnderecoFavorecido = movimento.PessoaEmpresaDestino.ComplementoEndereco;
                segmento.BairroFavorecido = movimento.PessoaEmpresaDestino.Bairro;
                segmento.CidadeFavorecido = movimento.PessoaEmpresaDestino.Cidade;
                segmento.CEPFavorecido = movimento.PessoaEmpresaDestino.CEP;
                segmento.UFFavorecido = movimento.PessoaEmpresaDestino.UF;

                return PreencheSegmentoB(segmento, movimento);
            }

            if (
                _tipoLancamento == TipoLancamentoEnum.PIXTransferencia
            )
            {
                var segmento = (SegmentoB_PIX)NovoSegmentoB(_tipoLancamento);

                segmento.LoteServico = numeroLote;

                segmento.FormaIniciacao = movimento.MovimentoPagamentoTransferencia.TipoChavePIX;
                segmento.ChavePIX = movimento.MovimentoPagamentoTransferencia.ChavePIX;

                return PreencheSegmentoB(segmento, movimento);
            }

            throw new Exception("Movimento de transferência não implementado para esta modalidade");
        }

        private RegistroDetalheBase PreencheSegmentoCBase(Movimento movimento, int numeroLote)
        {
            if (movimento.MovimentoPagamentoTransferencia == null)
                return null;

            var segmento = (SegmentoC)NovoSegmentoC(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoC(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoJBase(Movimento movimento, int numeroLote)
        {
            if (movimento.MovimentoTituloCodigoBarra == null)
                return null;

            var segmento = (SegmentoJ)NovoSegmentoJ(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            segmento.BancoCodigoBarra = movimento.MovimentoTituloCodigoBarra.BancoCodigoBarra;
            segmento.MoedaCodigoBarra = movimento.MovimentoTituloCodigoBarra.MoedaCodigoBarra;
            segmento.DVCodigoBarra = movimento.MovimentoTituloCodigoBarra.DVCodigoBarra;
            segmento.FatorVencimentoCodigoBarra = movimento.MovimentoTituloCodigoBarra.FatorVencimentoCodigoBarra;
            segmento.ValorCodigoBarra = movimento.MovimentoTituloCodigoBarra.ValorCodigoBarra;
            segmento.CampoLivreCodigoBarra = movimento.MovimentoTituloCodigoBarra.CampoLivreCodigoBarra;
            segmento.NomeBeneficiario = movimento.PessoaEmpresaDestino.Nome;

            var fator = segmento.FatorVencimentoCodigoBarra;
            var dataBase = DateTime.Parse("10/07/1997");
            segmento.DataVencimento = dataBase.AddDays(fator);

            segmento.ValorTitulo = movimento.MovimentoTituloCodigoBarra.ValorTitulo;
            segmento.ValorDesconto = movimento.MovimentoTituloCodigoBarra.Desconto;
            segmento.ValorAcrescimo = movimento.MovimentoTituloCodigoBarra.Acrescimo;
            segmento.DataPagamento = movimento.MovimentoTituloCodigoBarra.DataPagamento;
            segmento.ValorPagamento = movimento.MovimentoTituloCodigoBarra.ValorPagamento;
            segmento.QuantidadeMoeda = movimento.MovimentoTituloCodigoBarra.QuantidadeMoeda;
            segmento.CodigoDocumentoNaEmpresa = movimento.MovimentoTituloCodigoBarra.NumeroDocumento;

            return PreencheSegmentoJ(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoJ52Base(Movimento movimento, int numeroLote)
        {
            if (movimento.MovimentoTituloCodigoBarra == null)
                return null;

            var segmento = (SegmentoJ52_Boleto)NovoSegmentoJ52(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            segmento.TipoInscricaoSacado = _empresaCedente.TipoPessoa;
            segmento.InscricaoSacado = long.Parse(_empresaCedente.CPF_CNPJ.JustNumbers());
            segmento.NomeSacado = _empresaCedente.Nome;

            segmento.TipoInscricaoCedente = movimento.PessoaEmpresaDestino.TipoPessoa;
            segmento.InscricaoSacado = long.Parse(movimento.PessoaEmpresaDestino.CPF_CNPJ.JustNumbers());
            segmento.NomeSacado = movimento.PessoaEmpresaDestino.Nome;

            return PreencheSegmentoJ52(segmento, movimento);
        }

        #endregion

        #region ::. Métodos Herdáveis .::

        public virtual HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    return new HeaderLote_PagamentoTransferencia(this);
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    return new HeaderLote_PagamentoTitulo(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        public virtual RegistroDetalheBase NovoSegmentoA(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    return new SegmentoA_Transferencia(this);
                case TipoLancamentoEnum.PIXTransferencia:
                    return new SegmentoA_PIX_TITULO(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        public virtual RegistroDetalheBase NovoSegmentoB(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    return new SegmentoB_Transferencia(this);
                case TipoLancamentoEnum.PIXTransferencia:
                    return new SegmentoB_PIX(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        public virtual RegistroDetalheBase NovoSegmentoC(TipoLancamentoEnum tipoLancamento) => new SegmentoC(this);
        public virtual RegistroDetalheBase NovoSegmentoJ(TipoLancamentoEnum tipoLancamento) => new SegmentoJ(this);
        public virtual RegistroDetalheBase NovoSegmentoJ52(TipoLancamentoEnum tipoLancamento) => new SegmentoJ52_Boleto(this);

        public virtual TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        public virtual HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote) => headerLote;
        public virtual TrailerLoteBase PreencheTrailerLote(TrailerLoteBase trailerLote) => trailerLote;
        public virtual RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoJ52(RegistroDetalheBase segmento, Movimento movimento) => segmento;

        #endregion
    }
}

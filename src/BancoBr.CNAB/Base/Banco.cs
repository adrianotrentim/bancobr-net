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
        private Correntista _empresa;
        private TipoServicoEnum _tipoServico;
        private TipoLancamentoEnum _tipoLancamento;
        private LocalDebitoEnum _localDebito;

        protected Banco(int codigo, string nome, int versaoArquivo)
            : base(codigo, nome, versaoArquivo)
        {
        }

        public virtual RegistroBase NovoHeaderArquivo(Correntista correntista, int numeroRemessa) => new HeaderArquivo(this, correntista, numeroRemessa);
        public virtual RegistroBase NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Métodos Públicos .::

        public Lote NovoLote(Correntista correntista, TipoServicoEnum tipoServico, TipoLancamentoEnum tipoLancamento, LocalDebitoEnum localDebito)
        {
            _empresa = correntista;
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

            RegistroDetalheBase segmentoA = null;
            RegistroDetalheBase segmentoB = null;
            RegistroDetalheBase segmentoC = null;
            RegistroDetalheBase segmentoJ = null;
            RegistroDetalheBase segmentoJ52 = null;

            switch (_tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    segmentoA = PreencheSegmentoABase(movimento, numeroLote);
                    segmentoB = PreencheSegmentoBBase(movimento, numeroLote);
                    segmentoC = PreencheSegmentoCBase(movimento, numeroLote);
                    break;
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    segmentoJ = PreencheSegmentoJBase(movimento, numeroLote);
                    segmentoJ52 = PreencheSegmentoJ52Base(movimento, numeroLote);
                    break;
            }

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

            if (headerLote is HeaderLote_Transferencia)
            {
                ((HeaderLote_Transferencia)headerLote).Servico = _tipoServico;
                ((HeaderLote_Transferencia)headerLote).TipoLancamento = _tipoLancamento;
                ((HeaderLote_Transferencia)headerLote).TipoInscricaoEmpresa = _empresa.TipoPessoa;
                ((HeaderLote_Transferencia)headerLote).InscricaoEmpresa = long.Parse(_empresa.CPF_CNPJ.JustNumbers());
                ((HeaderLote_Transferencia)headerLote).NumeroAgencia = _empresa.NumeroAgencia;
                ((HeaderLote_Transferencia)headerLote).DVAgencia = _empresa.DVAgencia;
                ((HeaderLote_Transferencia)headerLote).NumeroConta = _empresa.NumeroConta;
                ((HeaderLote_Transferencia)headerLote).DVConta = _empresa.DVConta;
                ((HeaderLote_Transferencia)headerLote).Convenio = _empresa.Convenio;

                if (_empresa.DVConta.Length >= 2)
                {
                    ((HeaderLote_Transferencia)headerLote).DVConta = _empresa.DVConta.Substring(0, 1);
                    ((HeaderLote_Transferencia)headerLote).DVAgenciaConta = _empresa.DVConta.Substring(1, 1);
                }

                ((HeaderLote_Transferencia)headerLote).NomeEmpresa = _empresa.Nome;
                ((HeaderLote_Transferencia)headerLote).EnderecoEmpresa = _empresa.Endereco;
                ((HeaderLote_Transferencia)headerLote).NumeroEnderecoEmpresa = _empresa.NumeroEndereco;
                ((HeaderLote_Transferencia)headerLote).ComplementoEnderecoEmpresa = _empresa.ComplementoEndereco;
                ((HeaderLote_Transferencia)headerLote).CidadeEmpresa = _empresa.Cidade;
                ((HeaderLote_Transferencia)headerLote).CEPEmpresa = _empresa.CEP;
                ((HeaderLote_Transferencia)headerLote).UFEmpresa = _empresa.UF;
                ((HeaderLote_Transferencia)headerLote).LocalDebito = _localDebito;
            }
            else if (headerLote is HeaderLote_PagamentoTitulo)
            {
                ((HeaderLote_PagamentoTitulo)headerLote).Servico = _tipoServico;
                ((HeaderLote_PagamentoTitulo)headerLote).TipoLancamento = _tipoLancamento;
                ((HeaderLote_PagamentoTitulo)headerLote).TipoInscricaoEmpresa = _empresa.TipoPessoa;
                ((HeaderLote_PagamentoTitulo)headerLote).InscricaoEmpresa = long.Parse(_empresa.CPF_CNPJ.JustNumbers());
                ((HeaderLote_PagamentoTitulo)headerLote).NumeroAgencia = _empresa.NumeroAgencia;
                ((HeaderLote_PagamentoTitulo)headerLote).DVAgencia = _empresa.DVAgencia;
                ((HeaderLote_PagamentoTitulo)headerLote).NumeroConta = _empresa.NumeroConta;
                ((HeaderLote_PagamentoTitulo)headerLote).DVConta = _empresa.DVConta;
                ((HeaderLote_PagamentoTitulo)headerLote).Convenio = _empresa.Convenio;

                if (_empresa.DVConta.Length >= 2)
                {
                    ((HeaderLote_PagamentoTitulo)headerLote).DVConta = _empresa.DVConta.Substring(0, 1);
                    ((HeaderLote_PagamentoTitulo)headerLote).DVAgenciaConta = _empresa.DVConta.Substring(1, 1);
                }

                ((HeaderLote_PagamentoTitulo)headerLote).NomeEmpresa = _empresa.Nome;
                ((HeaderLote_PagamentoTitulo)headerLote).EnderecoEmpresa = _empresa.Endereco;
                ((HeaderLote_PagamentoTitulo)headerLote).NumeroEnderecoEmpresa = _empresa.NumeroEndereco;
                ((HeaderLote_PagamentoTitulo)headerLote).ComplementoEnderecoEmpresa = _empresa.ComplementoEndereco;
                ((HeaderLote_PagamentoTitulo)headerLote).CidadeEmpresa = _empresa.Cidade;
                ((HeaderLote_PagamentoTitulo)headerLote).CEPEmpresa = _empresa.CEP;
                ((HeaderLote_PagamentoTitulo)headerLote).UFEmpresa = _empresa.UF;
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
            if (
                (_tipoLancamento == TipoLancamentoEnum.TEDMesmaTitularidade ||
                _tipoLancamento == TipoLancamentoEnum.TEDOutraTitularidade)
                &&
                ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).CodigoFinalidadeTED == FinalidadeTEDEnum.NaoAplicavel
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

                    ((SegmentoA_Transferencia)segmento).BancoFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).Banco;
                    ((SegmentoA_Transferencia)segmento).AgenciaFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).NumeroAgencia;
                    ((SegmentoA_Transferencia)segmento).DVAgenciaFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).DVAgencia.Substring(0, 1);
                    ((SegmentoA_Transferencia)segmento).ContaFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).NumeroConta;
                    ((SegmentoA_Transferencia)segmento).DVContaFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).DVConta;

                    if (((MovimentoItemTransferenciaTED)movimento.MovimentoItem).DVConta.Length >= 2)
                    {
                        ((SegmentoA_Transferencia)segmento).DVContaFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).DVConta.Substring(0, 1);
                        ((SegmentoA_Transferencia)segmento).DVAgenciaContaFavorecido = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).DVConta.Substring(1, 1);
                    }

                    break;
                case TipoLancamentoEnum.PIXTransferencia:
                case TipoLancamentoEnum.PIXQrCode:
                    segmento.CamaraCentralizadora = 9;
                    break;
            }

            segmento.NomeFavorecido = movimento.Favorecido.Nome;
            segmento.NumeroDocumentoEmpresa = movimento.NumeroDocumento;
            segmento.DataPagamento = movimento.DataPagamento;
            segmento.ValorPagamento = movimento.ValorPagamento;
            segmento.TipoMoeda = movimento.Moeda;
            segmento.QuantidadeMoeda = movimento.QuantidadeMoeda;

            switch (segmento.CamaraCentralizadora)
            {
                case 18:
                    if (
                        _tipoLancamento == TipoLancamentoEnum.TEDMesmaTitularidade ||
                        _tipoLancamento == TipoLancamentoEnum.TEDOutraTitularidade ||
                        _tipoLancamento == TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco ||
                        _tipoLancamento == TipoLancamentoEnum.CreditoContaMesmoBanco
                    )
                        ((SegmentoA_Transferencia)segmento).CodigoFinalidadeTED = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).CodigoFinalidadeTED;
                    break;
            }

            segmento.AvisoFavorecido = movimento.AvisoAoFavorecido;

            return PreencheSegmentoA(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Movimento movimento, int numeroLote)
        {
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

                segmento.TipoInscricaoFavorecido = movimento.Favorecido.TipoPessoa;
                segmento.InscricaoFavorecido = long.Parse(movimento.Favorecido.CPF_CNPJ.JustNumbers());
                segmento.EnderecoFavorecido = movimento.Favorecido.Endereco;
                segmento.NumeroEnderecoFavorecido = movimento.Favorecido.NumeroEndereco;
                segmento.ComplementoEnderecoFavorecido = movimento.Favorecido.ComplementoEndereco;
                segmento.BairroFavorecido = movimento.Favorecido.Bairro;
                segmento.CidadeFavorecido = movimento.Favorecido.Cidade;
                segmento.CEPFavorecido = movimento.Favorecido.CEP;
                segmento.UFFavorecido = movimento.Favorecido.UF;

                return PreencheSegmentoB(segmento, movimento);
            }

            if (
                _tipoLancamento == TipoLancamentoEnum.PIXTransferencia
            )
            {
                var segmento = (SegmentoB_PIX)NovoSegmentoB(_tipoLancamento);

                segmento.LoteServico = numeroLote;

                segmento.FormaIniciacao = ((MovimentoItemTransferenciaPIX)movimento.MovimentoItem).TipoChavePIX;
                segmento.ChavePIX = ((MovimentoItemTransferenciaPIX)movimento.MovimentoItem).ChavePIX;

                return PreencheSegmentoB(segmento, movimento);
            }

            throw new Exception("Movimento de transferência não implementado para esta modalidade");
        }

        private RegistroDetalheBase PreencheSegmentoCBase(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoC)NovoSegmentoC(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoC(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoJBase(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoJ)NovoSegmentoJ(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            segmento.BancoCodigoBarra = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).BancoCodigoBarra;
            segmento.MoedaCodigoBarra = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).MoedaCodigoBarra;
            segmento.DVCodigoBarra = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).DVCodigoBarra;
            segmento.FatorVencimentoCodigoBarra = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).FatorVencimentoCodigoBarra;
            segmento.ValorCodigoBarra = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).ValorCodigoBarra;
            segmento.CampoLivreCodigoBarra = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).CampoLivreCodigoBarra;
            segmento.NomeBeneficiario = movimento.Favorecido.Nome;

            var fator = segmento.FatorVencimentoCodigoBarra;
            var dataBase = DateTime.Parse("10/07/1997");
            segmento.DataVencimento = dataBase.AddDays(fator);

            segmento.ValorTitulo = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).ValorCodigoBarra;
            segmento.ValorDesconto = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).Desconto;
            segmento.ValorAcrescimo = ((MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem).Acrescimo;
            segmento.DataPagamento = movimento.DataPagamento;
            segmento.ValorPagamento = movimento.ValorPagamento;
            segmento.QuantidadeMoeda = movimento.QuantidadeMoeda;
            segmento.CodigoDocumentoNaEmpresa = movimento.NumeroDocumento;

            return PreencheSegmentoJ(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoJ52Base(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoJ52_Boleto)NovoSegmentoJ52(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            segmento.TipoInscricaoSacado = _empresa.TipoPessoa;
            segmento.InscricaoSacado = long.Parse(_empresa.CPF_CNPJ.JustNumbers());
            segmento.NomeSacado = _empresa.Nome;

            segmento.TipoInscricaoCedente = movimento.Favorecido.TipoPessoa;
            segmento.InscricaoSacado = long.Parse(movimento.Favorecido.CPF_CNPJ.JustNumbers());
            segmento.NomeSacado = movimento.Favorecido.Nome;

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
                    return new HeaderLote_Transferencia(this);
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

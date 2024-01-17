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

            segmentoA.NumeroRegistro = numeroRegistro;
            registros.Add(segmentoA);

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

            return registros;
        }

        #endregion

        #region ::. Métodos Privados .::

        private HeaderLoteBase PreencheHeaderLoteBase()
        {
            var headerLote = (HeaderLote)NovoHeaderLote(_tipoLancamento);

            headerLote.Servico = _tipoServico;
            headerLote.TipoLancamento = _tipoLancamento;
            headerLote.TipoInscricaoEmpresa = _empresaCedente.TipoPessoa;
            headerLote.InscricaoEmpresa = long.Parse(_empresaCedente.CPF_CNPJ.JustNumbers());
            headerLote.NumeroAgencia = _empresaCedente.NumeroAgencia;
            headerLote.DVAgencia = _empresaCedente.DVAgencia;
            headerLote.NumeroConta = _empresaCedente.NumeroConta;
            headerLote.DVConta = _empresaCedente.DVConta;
            headerLote.Convenio = _empresaCedente.Convenio;

            if (_empresaCedente.DVConta.Length >= 2)
            {
                headerLote.DVConta = _empresaCedente.DVConta.Substring(0, 1);
                headerLote.DVAgenciaConta = _empresaCedente.DVConta.Substring(1, 1);
            }

            headerLote.NomeEmpresa = _empresaCedente.Nome;
            headerLote.EnderecoEmpresa = _empresaCedente.Endereco;
            headerLote.NumeroEnderecoEmpresa = _empresaCedente.NumeroEndereco;
            headerLote.ComplementoEnderecoEmpresa = _empresaCedente.ComplementoEndereco;
            headerLote.CidadeEmpresa = _empresaCedente.Cidade;
            headerLote.CEPEmpresa = _empresaCedente.CEP;
            headerLote.UFEmpresa = _empresaCedente.UF;
            headerLote.LocalDebito = _localDebito;

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
                movimento.CodigoFinalidadeTED == FinalidadeTEDEnum.NaoAplicavel
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
                    break;
                case TipoLancamentoEnum.PIXTransferencia:
                    segmento.CamaraCentralizadora = 9;
                    break;
                default:
                    segmento.CamaraCentralizadora = 0;
                    break;
            }

            segmento.BancoFavorecido = movimento.PessoaEmpresaDestino.Banco;
            segmento.AgenciaFavorecido = movimento.PessoaEmpresaDestino.NumeroAgencia;
            segmento.DVAgenciaFavorecido = movimento.PessoaEmpresaDestino.DVAgencia.Substring(0, 1);
            segmento.ContaFavorecido = movimento.PessoaEmpresaDestino.NumeroConta;
            segmento.DVContaFavorecido = movimento.PessoaEmpresaDestino.DVConta;

            if (movimento.PessoaEmpresaDestino.DVConta.Length >= 2)
            {
                segmento.DVContaFavorecido = movimento.PessoaEmpresaDestino.DVConta.Substring(0, 1);
                segmento.DVAgenciaContaFavorecido = movimento.PessoaEmpresaDestino.DVConta.Substring(1, 1);
            }

            segmento.NomeFavorecido = movimento.PessoaEmpresaDestino.Nome;
            segmento.NumeroDocumentoEmpresa = movimento.NumeroDocumento;
            segmento.DataPagamento = movimento.DataPagamento;
            segmento.TipoMoeda = movimento.Moeda;
            segmento.QuantidadeMoeda = movimento.QuantidadeMoeda;
            segmento.ValorPagamento = movimento.ValorPagamento;

            switch (segmento.CamaraCentralizadora)
            {
                case 18:
                    segmento.CodigoFinalidadeTED = movimento.CodigoFinalidadeTED;
                    break;
            }

            segmento.AvisoFavorecido = movimento.AvisoAoFavorecido;

            return PreencheSegmentoA(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoB)NovoSegmentoB(_tipoLancamento);

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

            return PreencheSegmentoJ(segmento, movimento);
        }

        #endregion

        #region ::. Métodos Herdáveis .::

        public virtual HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento) => new HeaderLote(this);
        public virtual RegistroDetalheBase NovoSegmentoA(TipoLancamentoEnum tipoLancamento) => new SegmentoA(this);
        public virtual RegistroDetalheBase NovoSegmentoB(TipoLancamentoEnum tipoLancamento) => new SegmentoB(this);
        public virtual RegistroDetalheBase NovoSegmentoC(TipoLancamentoEnum tipoLancamento) => new SegmentoC(this);
        public virtual RegistroDetalheBase NovoSegmentoJ(TipoLancamentoEnum tipoLancamento) => new SegmentoJ(this);
        public virtual TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        public virtual HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote) => headerLote;
        public virtual TrailerLoteBase PreencheTrailerLote(TrailerLoteBase trailerLote) => trailerLote;
        public virtual RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento) => segmento;

        #endregion
    }
}

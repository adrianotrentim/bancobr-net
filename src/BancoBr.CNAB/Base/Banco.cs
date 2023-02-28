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
        private FormaLancamentoEnum _formaLancamento;
        private TipoLancamentoEnum _tipoLancamento;

        protected Banco(int codigo, string nome, int versaoArquivo)
            : base(codigo, nome, versaoArquivo)
        {
        }

        public virtual RegistroBase NovoHeaderArquivo(Pessoa empresaCedente, int numeroRemessa) => new HeaderArquivo(this, empresaCedente, numeroRemessa);
        public virtual RegistroBase NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Métodos Públicos .::

        public Lote NovoLote(Pessoa empresaCedente, TipoServicoEnum tipoServico, FormaLancamentoEnum formaLancamento, TipoLancamentoEnum tipoLancamento)
        {
            _empresaCedente = empresaCedente;
            _tipoServico = tipoServico;
            _formaLancamento = formaLancamento;
            _tipoLancamento = tipoLancamento;

            if (formaLancamento == FormaLancamentoEnum.CartaoSalario && tipoServico != TipoServicoEnum.PagamentoSalarios)
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

            if (segmentoB != null) registros.Add(segmentoB);
            if (segmentoC != null) registros.Add(segmentoC);
            if (segmentoJ != null) registros.Add(segmentoJ);

            return registros;
        }

        #endregion

        #region ::. Métodos Privados .::

        private HeaderLoteBase PreencheHeaderLoteBase()
        {
            var headerLote = (HeaderLote)NovoHeaderLote(_formaLancamento);

            headerLote.Servico = _tipoServico;
            headerLote.FormaLancamento = _formaLancamento;
            headerLote.TipoInscricaoEmpresa = _empresaCedente.TipoPessoa;
            headerLote.InscricaoEmpresa = long.Parse(_empresaCedente.CPF_CNPJ.JustNumbers());
            headerLote.NumeroAgencia = _empresaCedente.NumeroAgencia;
            headerLote.DVAgencia = _empresaCedente.DVAgencia;
            headerLote.NumeroConta = _empresaCedente.NumeroConta;
            headerLote.DVConta = _empresaCedente.DVConta;

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
            headerLote.TipoLancamento = _tipoLancamento;

            PreencheHeaderLote(headerLote);

            return headerLote;
        }

        private TrailerLoteBase PreencheTrailerLoteBase(Lote lote)
        {
            var trailerLote = (TrailerLote)NovoTrailerLote(lote);

            PreencheTrailerLote(trailerLote);

            return trailerLote;
        }

        private RegistroDetalheBase PreencheSegmentoABase(Movimento movimento, int numeroLote)
        {
            if (_formaLancamento == FormaLancamentoEnum.DOC_TED && movimento.TipoDOCTED == TipoDOCTEDEnum.NaoAplicavel)
                throw new InvalidOperationException("Para a forma de movimento DOC / TED, você deve informar o Tipo de DOC ou TED");

            var segmento = (SegmentoA)NovoSegmentoA(_formaLancamento);

            segmento.LoteServico = numeroLote;
            segmento.TipoMovimento = movimento.TipoMovimento;
            segmento.CodigoInstrucaoMovimento = movimento.CodigoInstrucao;

            switch (_formaLancamento)
            {
                case FormaLancamentoEnum.DOC_TED when movimento.TipoDOCTED == TipoDOCTEDEnum.DOC:
                    segmento.CamaraCentralizadora = 18;
                    break;
                case FormaLancamentoEnum.DOC_TED when movimento.TipoDOCTED == TipoDOCTEDEnum.TED:
                    segmento.CamaraCentralizadora = 700;
                    break;
                case FormaLancamentoEnum.TEDMesmaTitularidade:
                case FormaLancamentoEnum.TEDOutraTitularidade:
                    segmento.CamaraCentralizadora = 18;
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
                    segmento.CodigoFinalidadeDOC = movimento.FinalidadeLancamento;
                    break;
                case 700:
                    segmento.CodigoFinalidadeTED = movimento.FinalidadeLancamento;
                    break;
            }

            segmento.AvisoFavorecido = movimento.AvisoAoFavorecido;

            return PreencheSegmentoA(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoB)NovoSegmentoB(_formaLancamento);

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoB(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoCBase(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoC)NovoSegmentoC(_formaLancamento);

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoC(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoJBase(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoJ)NovoSegmentoJ(_formaLancamento);

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoJ(segmento, movimento);
        }

        #endregion

        #region ::. Métodos Herdáveis .::

        public virtual HeaderLoteBase NovoHeaderLote(FormaLancamentoEnum formaLancamento) => new HeaderLote(this);
        public virtual RegistroDetalheBase NovoSegmentoA(FormaLancamentoEnum formaLancamento) => new SegmentoA(this);
        public virtual RegistroDetalheBase NovoSegmentoB(FormaLancamentoEnum formaLancamento) => new SegmentoB(this);
        public virtual RegistroDetalheBase NovoSegmentoC(FormaLancamentoEnum formaLancamento) => new SegmentoC(this);
        public virtual RegistroDetalheBase NovoSegmentoJ(FormaLancamentoEnum formaLancamento) => new SegmentoJ(this);
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

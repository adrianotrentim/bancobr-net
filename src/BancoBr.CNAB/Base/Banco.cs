using System;
using System.Collections.Generic;
using BancoBr.CNAB.Febraban;
using BancoBr.CNAB.Febraban.Transferencia;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public abstract class Banco : Common.Instances.Banco
    {
        private Pessoa _empresaCedente;
        private TipoServicoEnum _tipoServico;
        private FormaPagamentoEnum _formaPagamento;
        private TipoLancamentoEnum _tipoLancamento;

        protected Banco(int codigo, string nome, int versaoArquivo)
            : base(codigo, nome, versaoArquivo)
        {
        }

        public virtual RegistroBase NovoHeaderArquivo(Pessoa empresaCedente, int numeroRemessa) => new HeaderArquivo(this, empresaCedente, numeroRemessa);
        public virtual RegistroBase NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #region ::. Bloco de Transferências .::

        #region ::. Métodos Privados .::

        private HeaderLoteBase PreencheHeaderLoteBase()
        {
            var headerLote = (HeaderLote)NovoHeaderLote();

            headerLote.Servico = _tipoServico;
            headerLote.FormaPagamento = _formaPagamento;
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

        private RegistroDetalheBase PreencheSegmentoABase(Transferencia transferencia, int numeroLote)
        {
            if (_formaPagamento == FormaPagamentoEnum.DOC_TED && transferencia.TipoDOCTED == TipoDOCTEDEnum.NaoAplicavel)
                throw new InvalidOperationException("Para a forma de transferencia DOC / TED, você deve informar o Tipo de DOC ou TED");

            var segmento = (SegmentoA)NovoSegmentoA();

            segmento.LoteServico = numeroLote;
            segmento.TipoMovimento = transferencia.TipoMovimento;
            segmento.CodigoInstrucaoMovimento = transferencia.CodigoInstrucao;

            switch (_formaPagamento)
            {
                case FormaPagamentoEnum.DOC_TED when transferencia.TipoDOCTED == TipoDOCTEDEnum.DOC:
                    segmento.CamaraCentralizadora = 18;
                    break;
                case FormaPagamentoEnum.DOC_TED when transferencia.TipoDOCTED == TipoDOCTEDEnum.TED:
                    segmento.CamaraCentralizadora = 700;
                    break;
                case FormaPagamentoEnum.TEDMesmaTitularidade:
                case FormaPagamentoEnum.TEDOutraTitularidade:
                    segmento.CamaraCentralizadora = 18;
                    break;
                default:
                    segmento.CamaraCentralizadora = 0;
                    break;
            }

            segmento.BancoFavorecido = transferencia.PessoaEmpresaDestino.Banco;
            segmento.AgenciaFavorecido = transferencia.PessoaEmpresaDestino.NumeroAgencia;
            segmento.DVAgenciaFavorecido = transferencia.PessoaEmpresaDestino.DVAgencia.Substring(0, 1);
            segmento.ContaFavorecido = transferencia.PessoaEmpresaDestino.NumeroConta;
            segmento.DVContaFavorecido = transferencia.PessoaEmpresaDestino.DVConta;

            if (transferencia.PessoaEmpresaDestino.DVConta.Length >= 2)
            {
                segmento.DVContaFavorecido = transferencia.PessoaEmpresaDestino.DVConta.Substring(0, 1);
                segmento.DVAgenciaContaFavorecido = transferencia.PessoaEmpresaDestino.DVConta.Substring(1, 1);
            }

            segmento.NomeFavorecido = transferencia.PessoaEmpresaDestino.Nome;
            segmento.NumeroDocumentoEmpresa = transferencia.NumeroDocumento;
            segmento.DataPagamento = transferencia.DataPagamento;
            segmento.TipoMoeda = transferencia.Moeda;
            segmento.QuantidadeMoeda = transferencia.QuantidadeMoeda;
            segmento.ValorPagamento = transferencia.ValorPagamento;

            switch (segmento.CamaraCentralizadora)
            {
                case 18:
                    segmento.CodigoFinalidadeDOC = transferencia.FinalidadeLancamento;
                    break;
                case 700:
                    segmento.CodigoFinalidadeTED = transferencia.FinalidadeLancamento;
                    break;
            }

            segmento.AvisoFavorecido = transferencia.AvisoAoFavorecido;

            return PreencheSegmentoA(segmento, transferencia);
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Transferencia transferencia, int numeroLote)
        {
            var segmento = (SegmentoB)NovoSegmentoB();

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoB(segmento, transferencia);
        }

        private RegistroDetalheBase PreencheSegmentoCBase(Transferencia transferencia, int numeroLote)
        {
            var segmento = (SegmentoC)NovoSegmentoC();

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoC(segmento, transferencia);
        }

        private RegistroDetalheBase PreencheSegmentoJBase(Transferencia transferencia, int numeroLote)
        {
            var segmento = (SegmentoJ)NovoSegmentoJ();

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoJ(segmento, transferencia);
        }

        #endregion

        #region ::. Métodos Públicos .::

        public Lote NovoLoteTransferencia(Pessoa empresaCedente, TipoServicoEnum tipoServico, FormaPagamentoEnum formaPagamento, TipoLancamentoEnum tipoLancamento)
        {
            _empresaCedente = empresaCedente;
            _tipoServico = tipoServico;
            _formaPagamento = formaPagamento;
            _tipoLancamento = tipoLancamento;

            if (formaPagamento == FormaPagamentoEnum.CartaoSalario && tipoServico != TipoServicoEnum.PagamentoSalarios)
                throw new InvalidOperationException("A forma de transferencia Cartão Salário (4), só é permitida para o Tipo de Serviço Transferencia de Salários (30)");

            var lote = new Lote
            {
                Header = PreencheHeaderLoteBase()
            };

            lote.Trailer = PreencheTrailerLoteBase(lote);

            return lote;
        }

        public List<RegistroDetalheBase> NovaTransferencia(Transferencia titulo, int numeroLote, int numeroRegistro)
        {
            var registros = new List<RegistroDetalheBase>();

            var segmentoA = PreencheSegmentoABase(titulo, numeroLote);
            var segmentoB = PreencheSegmentoBBase(titulo, numeroLote);
            var segmentoC = PreencheSegmentoCBase(titulo, numeroLote);
            var segmentoJ = PreencheSegmentoJBase(titulo, numeroLote);

            segmentoA.NumeroRegistro = numeroRegistro;

            registros.Add(segmentoA);
            
            if (segmentoB != null) registros.Add(segmentoB);
            if (segmentoC != null) registros.Add(segmentoC);
            if (segmentoJ != null) registros.Add(segmentoJ);

            return registros;
        }

        #endregion

        #region ::. Métodos Herdáveis .::

        public virtual HeaderLoteBase NovoHeaderLote() => new HeaderLote(this);
        public virtual RegistroDetalheBase NovoSegmentoA() => new SegmentoA(this);
        public virtual RegistroDetalheBase NovoSegmentoB() => new SegmentoB(this);
        public virtual RegistroDetalheBase NovoSegmentoC() => new SegmentoC(this);
        public virtual RegistroDetalheBase NovoSegmentoJ() => new SegmentoJ(this);
        public virtual TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        public virtual HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote) => headerLote;
        public virtual TrailerLoteBase PreencheTrailerLote(TrailerLoteBase trailerLote) => trailerLote;
        public virtual RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Transferencia transferencia) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Transferencia transferencia) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Transferencia transferencia) => segmento;
        public virtual RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Transferencia transferencia) => segmento;

        #endregion

        #endregion
    }
}

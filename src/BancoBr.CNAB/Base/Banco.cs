using System;
using System.Collections.Generic;
using BancoBr.CNAB.Febraban.Pagamento;
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

        protected Banco(int codigo, string nome)
            : base(codigo, nome)
        {
        }

        protected Banco(int codigo, string nome, int versaoArquivo)
            : base(codigo, nome, versaoArquivo)
        {
        }

        #region ::. Bloco de Pagamentos .::

        public Lote NovoLotePagamento(Pessoa empresaCedente, TipoServicoEnum tipoServico, FormaPagamentoEnum formaPagamento, TipoLancamentoEnum tipoLancamento)
        {
            _empresaCedente = empresaCedente;
            _tipoServico = tipoServico;
            _formaPagamento = formaPagamento;
            _tipoLancamento = tipoLancamento;

            if (formaPagamento == FormaPagamentoEnum.CartaoSalario && tipoServico != TipoServicoEnum.PagamentoSalarios)
                throw new InvalidOperationException("A forma de pagamento Cartão Salário (4), só é permitida para o Tipo de Serviço Pagamento de Salários (30)");

            var lote = new Lote
            {
                Header = PreencheHeaderLoteBase()
            };

            lote.Trailer = PreencheTrailerLoteBase(lote);

            return lote;
        }

        public List<RegistroDetalheBase> NovoPagamento(Pagamento titulo, int numeroLote, int numeroRegistro)
        {
            var registros = new List<RegistroDetalheBase>();

            var segmentoA = PreencheSegmentoABase(titulo, numeroLote);
            var segmentoB = PreencheSegmentoBBase(titulo, numeroLote);
            var segmentoC = PreencheSegmentoCBase(titulo, numeroLote);

            segmentoA.NumeroRegistro = numeroRegistro;

            registros.Add(segmentoA);
            
            if (segmentoB != null) registros.Add(segmentoB);
            if (segmentoC != null) registros.Add(segmentoC);

            return registros;
        }

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

        private RegistroDetalheBase PreencheSegmentoABase(Pagamento pagamento, int numeroLote)
        {
            if (_formaPagamento == FormaPagamentoEnum.DOC_TED && pagamento.TipoDOCTED == TipoDOCTEDEnum.NaoAplicavel)
                throw new InvalidOperationException("Para a forma de pagamento DOC / TED, você deve informar o Tipo de DOC ou TED");

            var segmento = (SegmentoA)NovoSegmentoA();

            segmento.LoteServico = numeroLote;
            segmento.TipoMovimento = pagamento.TipoMovimento;
            segmento.CodigoInstrucaoMovimento = pagamento.CodigoInstrucao;

            switch (_formaPagamento)
            {
                case FormaPagamentoEnum.DOC_TED when pagamento.TipoDOCTED == TipoDOCTEDEnum.DOC:
                    segmento.CamaraCentralizadora = 18;
                    break;
                case FormaPagamentoEnum.DOC_TED when pagamento.TipoDOCTED == TipoDOCTEDEnum.TED:
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

            segmento.BancoFavorecido = pagamento.PessoaEmpresaDestino.Banco;
            segmento.AgenciaFavorecido = pagamento.PessoaEmpresaDestino.NumeroAgencia;
            segmento.DVAgenciaFavorecido = pagamento.PessoaEmpresaDestino.DVAgencia.Substring(0, 1);
            segmento.ContaFavorecido = pagamento.PessoaEmpresaDestino.NumeroConta;
            segmento.DVContaFavorecido = pagamento.PessoaEmpresaDestino.DVConta;

            if (pagamento.PessoaEmpresaDestino.DVConta.Length >= 2)
            {
                segmento.DVContaFavorecido = pagamento.PessoaEmpresaDestino.DVConta.Substring(0, 1);
                segmento.DVAgenciaContaFavorecido = pagamento.PessoaEmpresaDestino.DVConta.Substring(1, 1);
            }

            segmento.NomeFavorecido = pagamento.PessoaEmpresaDestino.Nome;
            segmento.NumeroDocumentoEmpresa = pagamento.NumeroDocumento;
            segmento.DataPagamento = pagamento.DataPagamento;
            segmento.TipoMoeda = pagamento.Moeda;
            segmento.QuantidadeMoeda = pagamento.QuantidadeMoeda;
            segmento.ValorPagamento = pagamento.ValorPagamento;

            switch (segmento.CamaraCentralizadora)
            {
                case 18:
                    segmento.CodigoFinalidadeDOC = pagamento.FinalidadeLancamento;
                    break;
                case 700:
                    segmento.CodigoFinalidadeTED = pagamento.FinalidadeLancamento;
                    break;
            }

            segmento.AvisoFavorecido = pagamento.AvisoAoFavorecido;

            return PreencheSegmentoA(segmento, pagamento);
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Pagamento pagamento, int numeroLote)
        {
            var segmento = (SegmentoB)NovoSegmentoB();
            
            segmento.LoteServico = numeroLote;

            return PreencheSegmentoB(segmento, pagamento);
        }

        private RegistroDetalheBase PreencheSegmentoCBase(Pagamento pagamento, int numeroLote)
        {
            var segmento = (SegmentoC)NovoSegmentoC();

            segmento.LoteServico = numeroLote;

            return PreencheSegmentoC(segmento, pagamento);
        }

        protected internal virtual HeaderLoteBase NovoHeaderLote()
        {
            return new HeaderLote(this);
        }
        
        protected internal virtual TrailerLoteBase NovoTrailerLote(Lote lote)
        {
            return new TrailerLote(lote);
        }

        protected internal virtual RegistroDetalheBase NovoSegmentoA()
        {
            return new SegmentoA(this);
        }

        protected internal virtual RegistroDetalheBase NovoSegmentoB()
        {
            return new SegmentoB(this);
        }

        protected internal virtual RegistroDetalheBase NovoSegmentoC()
        {
            return new SegmentoC(this);
        }

        protected internal virtual HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote)
        {
            return headerLote;
        }

        protected internal virtual TrailerLoteBase PreencheTrailerLote(TrailerLoteBase trailerLote)
        {
            return trailerLote;
        }

        protected internal virtual RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Pagamento pagamento)
        {
            return segmento;
        }

        protected internal virtual RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Pagamento pagamento)
        {
            return segmento;
        }

        protected internal virtual RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Pagamento pagamento)
        {
            return segmento;
        }

        #endregion
    }
}

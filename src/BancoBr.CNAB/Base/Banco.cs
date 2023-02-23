using System.Collections.Generic;
using BancoBr.CNAB.Febraban.Pagamento;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Base
{
    public abstract class Banco : Common.Instances.Banco
    {
        private Pessoa _empresaCedente;
        private int _tipoServico;
        private FormaLancamentoEnum _formaLancamento;
        private TipoFormaPagamentoEnum _tipoFormaPagamento;

        protected Banco(int codigo, string nome)
            : base(codigo, nome)
        {
        }

        protected Banco(int codigo, string nome, int versaoArquivo)
            : base(codigo, nome, versaoArquivo)
        {
        }

        #region ::. Bloco de Pagamentos .::

        public Lote NovoLotePagamento(Pessoa empresaCedente, int tipoServico, FormaLancamentoEnum formaLancamento, TipoFormaPagamentoEnum tipoFormaPagamento)
        {
            _empresaCedente = empresaCedente;
            _tipoServico = tipoServico;
            _formaLancamento = formaLancamento;
            _tipoFormaPagamento = tipoFormaPagamento;

            var lote = new Lote
            {
                Header = PreencheHeaderLoteBase()
            };

            return lote;
        }

        public List<RegistroBase> NovoPagamento(Pagamento titulo, int numeroRegistro)
        {
            var registros = new List<RegistroBase>();

            var segmentoA = PreencheSegmentoABase(titulo);
            var segmentoB = PreencheSegmentoBBase(titulo);
            var segmentoC = PreencheSegmentoCBase(titulo);

            segmentoA.NumeroRegistro = numeroRegistro;

            registros.Add(segmentoA);
            registros.Add(segmentoB);
            if (segmentoC != null) registros.Add(segmentoC);

            return registros;
        }

        private HeaderLoteBase PreencheHeaderLoteBase()
        {
            var headerLote = (HeaderLote)NovoHeaderLote();

            headerLote.Servico = _tipoServico;
            headerLote.FormaLancamento = _formaLancamento;
            headerLote.TipoInscricaoEmpresa = _empresaCedente.TipoPessoa;
            headerLote.InscricaoEmpresa = _empresaCedente.CPF_CNPJ;
            headerLote.NumeroAgencia = _empresaCedente.NumeroAgencia;
            headerLote.DVAgencia = _empresaCedente.DVAgencia;
            headerLote.NumeroConta = _empresaCedente.NumeroConta;
            headerLote.DVConta = _empresaCedente.DVConta;
            headerLote.DVAgenciaConta = _empresaCedente.DVConta.Substring(0, 1);

            if (_empresaCedente.DVConta.Length >= 2)
                headerLote.DVAgenciaConta = _empresaCedente.DVConta.Substring(1, 1);

            headerLote.EnderecoEmpresa = _empresaCedente.Endereco;
            headerLote.NumeroEnderecoEmpresa = _empresaCedente.NumeroEndereco;
            headerLote.ComplementoEnderecoEmpresa = _empresaCedente.ComplementoEndereco;
            headerLote.CidadeEmpresa = _empresaCedente.Cidade;
            headerLote.CEPEmpresa = _empresaCedente.CEP;
            headerLote.UFEmpresa = _empresaCedente.UF;
            headerLote.IndicativoFormaPagamento = _tipoFormaPagamento;

            PreencheHeaderLote(headerLote);

            return headerLote;
        }

        private RegistroDetalheBase PreencheSegmentoABase(Pagamento pagamento)
        {
            var segmento = (SegmentoA)NovoSegmentoA();

            PreencheSegmentoA(segmento, pagamento);

            return segmento;
        }

        private RegistroDetalheBase PreencheSegmentoBBase(Pagamento pagamento)
        {
            var segmento = (SegmentoB)NovoSegmentoB();

            PreencheSegmentoB(segmento, pagamento);

            return segmento;
        }

        private RegistroDetalheBase PreencheSegmentoCBase(Pagamento pagamento)
        {
            var segmento = (SegmentoC)NovoSegmentoC();

            PreencheSegmentoC(segmento, pagamento);

            return segmento;
        }

        protected internal virtual HeaderLoteBase NovoHeaderLote()
        {
            return new HeaderLote(this);
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

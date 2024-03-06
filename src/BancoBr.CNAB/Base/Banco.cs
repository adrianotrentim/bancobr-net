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
        private TipoServicoEnum _tipoServico;
        private TipoLancamentoEnum _tipoLancamento;
        private LocalDebitoEnum _localDebito;

        protected Banco(Correntista empresa, int codigo, string nome, int versaoArquivo)
            : base(empresa, codigo, nome, versaoArquivo)
        {
        }
        
        #region ::. Métodos Públicos .::

        internal Lote NovoLote(TipoServicoEnum tipoServico, TipoLancamentoEnum tipoLancamento, LocalDebitoEnum localDebito)
        {
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

        internal List<RegistroDetalheBase> NovoMovimento(Movimento movimento, int numeroLote, int numeroRegistro)
        {
            var registros = new List<RegistroDetalheBase>();

            RegistroDetalheBase segmentoA = null;
            RegistroDetalheBase segmentoB = null;
            RegistroDetalheBase segmentoC = null;
            RegistroDetalheBase segmentoJ = null;
            RegistroDetalheBase segmentoJ52 = null;
            RegistroDetalheBase segmentoO = null;

            switch (_tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.PIXTransferencia:
                    segmentoA = PreencheSegmentoABase(movimento, numeroLote);
                    segmentoB = PreencheSegmentoBBase(movimento, numeroLote);
                    break;
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    segmentoJ = PreencheSegmentoJBase(movimento, numeroLote);
                    segmentoJ52 = PreencheSegmentoJ52Base(movimento, numeroLote);
                    break;
                case TipoLancamentoEnum.PagamentoTributosCodigoBarra:
                    segmentoO = PreencheSegmentoOBase(movimento, numeroLote);
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

            if (segmentoO != null)
            {
                numeroRegistro++;
                segmentoO.NumeroRegistro = numeroRegistro;
                registros.Add(segmentoO);
            }

            return registros;
        }

        #endregion

        #region ::. Métodos Privados .::

        private HeaderArquivo PreencheHeaderArquivoBase(HeaderArquivo headerArquivo, int numeroRemessa, List<Movimento> movimentos)
        {
            headerArquivo.LoteServico = 0;
            headerArquivo.TipoRegistro = 0;
            headerArquivo.NumeroSequencialArquivo = numeroRemessa;
            headerArquivo.DataGeracao = DateTime.Now;
            headerArquivo.HoraGeracao = DateTime.Now;
            headerArquivo.TipoRemessaRetorno = TipoArquivoEnum.Remessa;
            headerArquivo.VersaoArquivo = VersaoArquivo;
            headerArquivo.NomeBanco = Nome;
            headerArquivo.DensidadeArquivo = 6250;

            if (Empresa != null)
            {
                headerArquivo.TipoInscricaoCpfcnpj = Empresa.TipoPessoa;
                headerArquivo.InscricaoEmpresa = long.Parse(Empresa.CPF_CNPJ.JustNumbers());
                headerArquivo.Convenio = Empresa.Convenio;
                headerArquivo.NumeroAgencia = Empresa.NumeroAgencia;
                headerArquivo.NomeEmpresa = Empresa.Nome;
                headerArquivo.DVAgencia = Empresa.DVAgencia;
                headerArquivo.NumeroConta = Empresa.NumeroConta;
                headerArquivo.DVConta = Empresa.DVConta.Substring(0, 1);

                if (Empresa.DVConta.Length >= 2)
                    headerArquivo.DVAgenciaConta = Empresa.DVConta.Substring(1, 1);
            }

            return PreencheHeaderArquivo(headerArquivo, movimentos);
        }

        private HeaderLoteBase PreencheHeaderLoteBase()
        {
            var headerLote = NovoHeaderLote(_tipoLancamento);

            ((HeaderLote)headerLote).Servico = _tipoServico;
            ((HeaderLote)headerLote).TipoLancamento = _tipoLancamento;
            ((HeaderLote)headerLote).TipoInscricaoEmpresa = Empresa.TipoPessoa;
            ((HeaderLote)headerLote).InscricaoEmpresa = long.Parse(Empresa.CPF_CNPJ.JustNumbers());
            ((HeaderLote)headerLote).NumeroAgencia = Empresa.NumeroAgencia;
            ((HeaderLote)headerLote).DVAgencia = Empresa.DVAgencia;
            ((HeaderLote)headerLote).NumeroConta = Empresa.NumeroConta;
            ((HeaderLote)headerLote).DVConta = Empresa.DVConta;
            ((HeaderLote)headerLote).Convenio = Empresa.Convenio;

            if (Empresa.DVConta.Length >= 2)
            {
                ((HeaderLote)headerLote).DVConta = Empresa.DVConta.Substring(0, 1);
                ((HeaderLote)headerLote).DVAgenciaConta = Empresa.DVConta.Substring(1, 1);
            }

            ((HeaderLote)headerLote).NomeEmpresa = Empresa.Nome;
            ((HeaderLote)headerLote).EnderecoEmpresa = Empresa.Endereco;
            ((HeaderLote)headerLote).NumeroEnderecoEmpresa = Empresa.NumeroEndereco;
            ((HeaderLote)headerLote).ComplementoEnderecoEmpresa = Empresa.ComplementoEndereco;
            ((HeaderLote)headerLote).CidadeEmpresa = Empresa.Cidade;
            ((HeaderLote)headerLote).CEPEmpresa = Empresa.CEP;
            ((HeaderLote)headerLote).UFEmpresa = Empresa.UF;

            if (headerLote is HeaderLote_TransferenciaConvenio)
            {
                ((HeaderLote_TransferenciaConvenio)headerLote).LocalDebito = _localDebito;
            }

            return PreencheHeaderLote(headerLote);
        }

        private RegistroDetalheBase PreencheSegmentoABase(Movimento movimento, int numeroLote)
        {
            #region ::. Validações .::

            if (
                (
                    _tipoLancamento == TipoLancamentoEnum.TEDMesmaTitularidade ||
                    _tipoLancamento == TipoLancamentoEnum.TEDOutraTitularidade
                ) &&
                ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).CodigoFinalidadeTED == FinalidadeTEDEnum.NaoAplicavel
            )
                throw new InvalidOperationException("Para a forma de movimento TED, você deve informar uma finalidade!");

            #endregion

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
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com código de instrução inválido!\r\nPara movimento de inclusão, favor utilizar os códigos de instrução:\r\n" +
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
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com código de instrução inválido!\r\nPara movimento de alteração, favor utilizar os códigos de instrução:\r\n" +
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
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:

                    if (
                        ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).Banco == 0 ||
                        ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).NumeroAgencia == 0 ||
                        ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).NumeroConta == 0
                    )
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com os dados para transferência ausentes (Banco, Agencia, Conta Corrente)!");
                    
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
                if (movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ && !movimento.Favorecido.CPF_CNPJ.IsValidCNPJ())
                    throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como transferência, mas o CNPJ do favorecido está inválido!");

                if (movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CPF && !movimento.Favorecido.CPF_CNPJ.IsValidCPF())
                    throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como transferência, mas o CPF do favorecido está inválido!");

                if (string.IsNullOrWhiteSpace(movimento.Favorecido.Nome))
                    throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como transferência, mas o Nome do favorecido não foi informado!");

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
                var movimentoItem = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;

                if (string.IsNullOrWhiteSpace(movimentoItem.ChavePIX))
                    throw new Exception($"A chave PIX não foi informada no movimento {movimento.NumeroDocumento}!");

                if (movimentoItem.TipoChavePIX == FormaIniciacaoEnum.PIX_Email && !movimentoItem.ChavePIX.IsValidEmail())
                    throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como PIX para e-mail, mas o e-mail está inválido!");

                if (movimentoItem.TipoChavePIX == FormaIniciacaoEnum.PIX_Telefone && (movimentoItem.ChavePIX.JustNumbers().Length < 10 || movimentoItem.ChavePIX.JustNumbers().Length > 11))
                    throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como PIX para celular, mas o número parece estar inválido!");

                if (movimentoItem.TipoChavePIX == FormaIniciacaoEnum.PIX_CPF_CNPJ && !movimentoItem.ChavePIX.IsValidCPFCNPJ())
                    throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como PIX para CPF ou CNPJ, mas o número está inválido!");

                var segmento = (SegmentoB_PIX)NovoSegmentoB(_tipoLancamento);

                segmento.LoteServico = numeroLote;

                segmento.TipoInscricaoFavorecido = movimento.Favorecido.TipoPessoa;
                segmento.InscricaoFavorecido = long.Parse(movimento.Favorecido.CPF_CNPJ.JustNumbers());
                segmento.FormaIniciacao = ((MovimentoItemTransferenciaPIX)movimento.MovimentoItem).TipoChavePIX;
                segmento.ChavePIX = ((MovimentoItemTransferenciaPIX)movimento.MovimentoItem).ChavePIX;

                return PreencheSegmentoB(segmento, movimento);
            }

            throw new Exception("Movimento de transferência não implementado para esta modalidade");
        }

        private RegistroDetalheBase PreencheSegmentoJBase(Movimento movimento, int numeroLote)
        {
            var movimentoItem = movimento.MovimentoItem as MovimentoItemPagamentoTituloCodigoBarra;

            #region ::. Validações .::

            if (
                movimentoItem.BancoCodigoBarra == 0 ||
                movimentoItem.MoedaCodigoBarra == 0 ||
                movimentoItem.FatorVencimentoCodigoBarra == 0 ||
                string.IsNullOrWhiteSpace(movimentoItem.CampoLivreCodigoBarra)
                
            )
                throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como pagamento de boleto, mas os dados do título estão ausentes!");

            if (movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ && !movimento.Favorecido.CPF_CNPJ.IsValidCNPJ())
                throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como pagamento de boleto, mas o CNPJ do favorecido está inválido!");

            if (movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CPF && !movimento.Favorecido.CPF_CNPJ.IsValidCPF())
                throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como pagamento de boleto, mas o CPF do favorecido está inválido!");

            if (string.IsNullOrWhiteSpace(movimento.Favorecido.Nome))
                throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como pagamento de boleto, mas o Nome do favorecido não foi informado!");

            #endregion

            var segmento = (SegmentoJ)NovoSegmentoJ(_tipoLancamento);

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
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com código de instrução inválido!\r\nPara movimento de inclusão, favor utilizar os códigos de instrução:\r\n" +
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
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com código de instrução inválido!\r\nPara movimento de alteração, favor utilizar os códigos de instrução:\r\n" +
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

            segmento.BancoCodigoBarra = movimentoItem.BancoCodigoBarra;
            segmento.MoedaCodigoBarra = movimentoItem.MoedaCodigoBarra;
            segmento.DVCodigoBarra = movimentoItem.DVCodigoBarra;
            segmento.FatorVencimentoCodigoBarra = movimentoItem.FatorVencimentoCodigoBarra;
            segmento.ValorCodigoBarra = movimentoItem.ValorCodigoBarra;
            segmento.CampoLivreCodigoBarra = movimentoItem.CampoLivreCodigoBarra;
            segmento.NomeBeneficiario = movimento.Favorecido.Nome;

            var fator = segmento.FatorVencimentoCodigoBarra;
            var dataBase = DateTime.Parse("07/10/1997");
            segmento.DataVencimento = dataBase.AddDays(fator);

            segmento.ValorTitulo = movimentoItem.ValorCodigoBarra;
            segmento.ValorDesconto = movimentoItem.Desconto;
            segmento.ValorAcrescimo = movimentoItem.Acrescimo;
            segmento.DataPagamento = movimento.DataPagamento;
            segmento.ValorPagamento = movimento.ValorPagamento;
            segmento.QuantidadeMoeda = movimento.QuantidadeMoeda;
            segmento.NumeroDocumentoEmpresa = movimento.NumeroDocumento;
            segmento.CodigoMoeda = movimentoItem.MoedaCodigoBarra;

            return PreencheSegmentoJ(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoJ52Base(Movimento movimento, int numeroLote)
        {
            var segmento = (SegmentoJ52_Boleto)NovoSegmentoJ52(_tipoLancamento);

            segmento.LoteServico = numeroLote;

            segmento.TipoInscricaoSacado = Empresa.TipoPessoa;
            segmento.InscricaoSacado = long.Parse(Empresa.CPF_CNPJ.JustNumbers());
            segmento.NomeSacado = Empresa.Nome;

            segmento.TipoInscricaoCedente = movimento.Favorecido.TipoPessoa;
            segmento.InscricaoCedente = long.Parse(movimento.Favorecido.CPF_CNPJ.JustNumbers());
            segmento.NomeCedente = movimento.Favorecido.Nome;

            return PreencheSegmentoJ52(segmento, movimento);
        }

        private RegistroDetalheBase PreencheSegmentoOBase(Movimento movimento, int numeroLote)
        {
            var movimentoItem = movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra;

            #region ::. Validações .::

            if (
                string.IsNullOrWhiteSpace(movimentoItem.CodigoBarra) ||
                movimentoItem.CodigoBarra?.Length != 44

            )
                throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como pagamento de convênios, mas o código de barras está inválido!");

            if (string.IsNullOrWhiteSpace(movimento.Favorecido.Nome))
                throw new Exception($"O movimento {movimento.NumeroDocumento} está sinalizado como pagamento de convênios, mas o Nome do favorecido não foi informado!");

            #endregion

            var segmento = (SegmentoO)NovoSegmentoO(_tipoLancamento);

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
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com código de instrução inválido!\r\nPara movimento de inclusão, favor utilizar os códigos de instrução:\r\n" +
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
                        throw new Exception($"O movimento {movimento.NumeroDocumento} está com código de instrução inválido!\r\nPara movimento de alteração, favor utilizar os códigos de instrução:\r\n" +
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

            segmento.CodigoBarra = movimentoItem.CodigoBarra;
            segmento.NomeBeneficiario = movimento.Favorecido.Nome;
            segmento.DataVencimento = movimentoItem.DataVencimento;
            segmento.DataPagamento = movimento.DataPagamento;
            segmento.ValorPagamento = movimento.ValorPagamento;
            segmento.NumeroDocumentoEmpresa = movimento.NumeroDocumento;
            
            return PreencheSegmentoO(segmento, movimento);
        }

        private TrailerLoteBase PreencheTrailerLoteBase(Lote lote)
        {
            var trailerLote = (TrailerLote)NovoTrailerLote(lote);

            return PreencheTrailerLote(trailerLote);
        }

        #endregion

        #region ::. Métodos para Criação de Instancias .::

        /*************************************************************************************************************************************
         *
         * Utilize estes métodos, quando for necessário alterações nas classes principais dos registros,
         * como por exemplo quando um determinado banco possuir campos no SegmentoA diferentes do padrão Febraban,
         * você poderá escrever uma nova classe SegmentoA com tais alterações, e retornada utilizando override nos métodos abaixo.
         *
         *************************************************************************************************************************************/

        internal virtual HeaderArquivo NovoHeaderArquivo(int numeroRemessa, List<Movimento> movimentos)
        {
            var headerArquivo = new HeaderArquivo(this);

            return PreencheHeaderArquivoBase(headerArquivo, numeroRemessa, movimentos);
        }

        internal virtual HeaderLoteBase NovoHeaderLote(TipoLancamentoEnum tipoLancamento)
        {
            HeaderLote headerLote;

            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.PIXTransferencia:
                    headerLote = new HeaderLote_TransferenciaConvenio(this)
                    {
                        VersaoLote = 46,
                        Operacao = "C"
                    };
                    break;
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    headerLote = new HeaderLote(this)
                    {
                        VersaoLote = 40,
                        Operacao = "C"
                    };
                    break;
                case TipoLancamentoEnum.PagamentoTributosCodigoBarra:
                    headerLote = new HeaderLote_TransferenciaConvenio(this)
                    {
                        VersaoLote = 12,
                        Operacao = "C"
                    };
                    return headerLote;
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }

            return headerLote;
        }

        internal virtual RegistroDetalheBase NovoSegmentoA(TipoLancamentoEnum tipoLancamento)
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
                    return new SegmentoA_PIX(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        internal virtual RegistroDetalheBase NovoSegmentoB(TipoLancamentoEnum tipoLancamento)
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

        internal virtual RegistroDetalheBase NovoSegmentoJ(TipoLancamentoEnum tipoLancamento) => new SegmentoJ(this);

        internal virtual RegistroDetalheBase NovoSegmentoJ52(TipoLancamentoEnum tipoLancamento) => new SegmentoJ52_Boleto(this);

        internal virtual RegistroDetalheBase NovoSegmentoO(TipoLancamentoEnum tipoLancamento) => new SegmentoO(this);

        internal virtual TrailerLoteBase NovoTrailerLote(Lote lote) => new TrailerLote(lote);

        internal virtual TrailerArquivo NovoTrailerArquivo(ArquivoCNAB arquivoCnab, List<Lote> lotes) => new TrailerArquivo(arquivoCnab, lotes);

        #endregion

        #region ::. Métodos para complementar dados nos registros .::

        /*************************************************************************************************************************************
         *
         * Utilize estes métodos, quando for necessário complementar somente dados nas classes principais dos registros,
         * como por exemplo quando um determinado banco possuir dados específicos no SegmentoA diferentes do padrão Febraban,
         * você poderá em sua classe Banco herdar o método que necessitar e  complementar ou alterar tais dados.
         *
         *************************************************************************************************************************************/

        internal virtual HeaderArquivo PreencheHeaderArquivo(HeaderArquivo headerArquivo, List<Movimento> movimentos) => headerArquivo;
        internal virtual HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote) => headerLote;
        internal virtual RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        internal virtual RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        internal virtual RegistroDetalheBase PreencheSegmentoJ(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        internal virtual RegistroDetalheBase PreencheSegmentoJ52(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        internal virtual RegistroDetalheBase PreencheSegmentoO(RegistroDetalheBase segmento, Movimento movimento) => segmento;
        internal virtual TrailerLoteBase PreencheTrailerLote(TrailerLoteBase trailerLote) => trailerLote;

        #endregion
    }
}

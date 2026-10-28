using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Core.Operacoes;
using BancoBr.API.Operacoes;
using BancoBr.Common.Instances;

namespace BancoBr.API
{
    public sealed partial class BancoApi
    {
        /// <summary>Envia usando o fluxo já implementado para o banco e devolve estado de execução, autorização e respostas originais. Preserve IdOperacao antes do envio e nos reenvios.</summary>
        public async Task<ResultadoPagamento> EnviarPagamentoAsync(RequisicaoPagamento requisicao, CancellationToken cancellationToken = default)
        {
            if (requisicao == null) throw new ArgumentNullException(nameof(requisicao));
            if (requisicao.IdOperacao == Guid.Empty) throw new ArgumentException("IdOperacao deve ser gerado e persistido antes do envio.", nameof(requisicao));
            ExtrairItem(requisicao.Movimento);
            // Consulta prévia pode gerar identificadores, mas a identidade do lançamento continua sendo a do consumidor.
            var metadados = MetadadosOperacao.De(requisicao.Movimento);
            var identidade = requisicao.IdOperacao.ToString("D");
            if (metadados.IdRequisicao != null && metadados.IdRequisicao != identidade)
                throw new ArgumentException("O movimento já pertence a outra operação. Preserve IdOperacao nos reenvios.", nameof(requisicao));
            metadados.IdRequisicao = identidade;
            using (var captura = new CapturaRetorno())
            {
                var movimento = await EnviarAgendamentoAsync(requisicao.Movimento, identidade, requisicao.Unidade, cancellationToken).ConfigureAwait(false);
                return NormalizadorResultado.Criar(movimento, ReferenciaDe(movimento, requisicao.IdOperacao, requisicao.Unidade), captura.Retornos);
            }
        }

        /// <summary>Consulta uma referência persistida, usando a mesma conta/banco da operação original.</summary>
        public async Task<ResultadoPagamento> ConsultarPagamentoAsync(ReferenciaPagamento referencia, CancellationToken cancellationToken = default)
        {
            var movimento = MovimentoDe(referencia);
            using (var captura = new CapturaRetorno())
            {
                movimento = await ConsultarReferenciaAsync(movimento, referencia, cancellationToken).ConfigureAwait(false);
                return NormalizadorResultado.Criar(movimento, ReferenciaDe(movimento, referencia.IdOperacao, referencia.Unidade), captura.Retornos);
            }
        }

        /// <summary>Cancela quando a modalidade e o estado permitem, preservando as restrições do banco.</summary>
        public async Task<ResultadoPagamento> CancelarPagamentoAsync(ReferenciaPagamento referencia, CancellationToken cancellationToken = default)
        {
            var movimento = MovimentoDe(referencia);
            using (var captura = new CapturaRetorno())
            {
                await CancelarAgendamentoAsync(movimento, referencia.IdOperacao.ToString("D"), cancellationToken).ConfigureAwait(false);
                return NormalizadorResultado.Criar(movimento, ReferenciaDe(movimento, referencia.IdOperacao, referencia.Unidade), captura.Retornos);
            }
        }

        /// <summary>Obtém o comprovante disponível: PDF Sicredi, link Santander ou autenticação/dados Sicoob/BB. A ausência de PDF não é presumida como erro.</summary>
        public async Task<ResultadoComprovante> ObterComprovanteAsync(ReferenciaPagamento referencia, CancellationToken cancellationToken = default)
        {
            var movimento = MovimentoDe(referencia);
            using (var captura = new CapturaRetorno())
            {
                byte[] conteudo = null;
                movimento = await ConsultarReferenciaAsync(movimento, referencia, cancellationToken).ConfigureAwait(false);
                if (movimento.SituacaoBancoBr == Common.Enums.BancoBrSituacaoEnum.Efetivado)
                {
                    if (_comprovanteBinario != null)
                        conteudo = await BaixarComprovanteAsync(movimento, cancellationToken).ConfigureAwait(false);
                    else if (_comprovante != null)
                        await ConsultarComprovanteAsync(movimento, cancellationToken).ConfigureAwait(false);
                }
                if (conteudo == null && movimento.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra convenio && !string.IsNullOrEmpty(convenio.ComprovanteBase64))
                    conteudo = Convert.FromBase64String(convenio.ComprovanteBase64);
                return new ResultadoComprovante
                {
                    Pagamento = NormalizadorResultado.Criar(movimento, ReferenciaDe(movimento, referencia.IdOperacao, referencia.Unidade), captura.Retornos),
                    Conteudo = conteudo, Link = movimento.LinkComprovanteBancoBr, ValidadeLink = movimento.ValidadeLinkComprovanteBancoBr,
                    Autenticacao = movimento.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra boleto ? boleto.NumeroAutenticacaoPagamento
                        : movimento.MovimentoItem is MovimentoItemTransferenciaTED ted ? ted.CodigoAutenticacao
                        : (movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra)?.Autenticacao
                };
            }
        }

        /// <summary>Consulta extrato com os mesmos filtros e movimentos antigos, acrescentando saldo por lançamento e as respostas de todas as páginas.</summary>
        public async Task<ResultadoExtrato> ConsultarExtratoDetalhadoAsync(RequisicaoExtrato requisicao, CancellationToken cancellationToken = default)
        {
            if (requisicao == null) throw new ArgumentNullException(nameof(requisicao));
            using (var captura = new CapturaRetorno())
            {
                var movimentos = await ConsultarExtratoAsync(requisicao.NumeroContaCorrente, requisicao.Mes, requisicao.Ano,
                    requisicao.DiaInicial, requisicao.DiaFinal, requisicao.AgruparCnab, cancellationToken).ConfigureAwait(false);
                return new ResultadoExtrato
                {
                    Movimentos = movimentos,
                    Lancamentos = movimentos.Where(m => m.MovimentoItem is MovimentoItemExtratoTransacao).Select(m => new LancamentoExtrato
                    {
                        Movimento = m, SaldoAposLancamento = MetadadosOperacao.De(m.MovimentoItem).SaldoAposLancamento
                    }).ToArray(), RetornosBanco = captura.Retornos.ToArray()
                };
            }
        }

        private Task<Movimento> ConsultarReferenciaAsync(Movimento movimento, ReferenciaPagamento referencia, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco) && !string.IsNullOrWhiteSpace(referencia.IdentidadeConsulta))
                return Requer(_boleto).ConsultarComprovantePorIdempotencyAsync(referencia.IdentidadeConsulta, cancellationToken);
            return ConsultarAgendamentoAsync(movimento, referencia.Unidade, cancellationToken);
        }

        private string ContaReferencia => _origem.NumeroAgencia.ToString(CultureInfo.InvariantCulture) + "/" + _origem.NumeroConta.ToString(CultureInfo.InvariantCulture) + "/" + _origem.DVConta;

        private ReferenciaPagamento ReferenciaDe(Movimento movimento, Guid idOperacao, int unidade)
        {
            var modalidade = movimento.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra ? ModalidadePagamento.Boleto
                : movimento.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra ? ModalidadePagamento.Convenio
                : movimento.MovimentoItem is MovimentoItemTransferenciaTED ? ModalidadePagamento.Ted
                : movimento.MovimentoItem is MovimentoItemTransferenciaPIX ? ModalidadePagamento.Pix : ModalidadePagamento.PixQrCode;
            return new ReferenciaPagamento
            {
                Banco = _origem.Banco, ContaOrigem = ContaReferencia, Modalidade = modalidade, IdOperacao = idOperacao,
                IdentificadorBanco = movimento.NumeroDocumentoNoBanco,
                IdentidadeConsulta = MetadadosOperacao.De(movimento).IdentidadeConsulta,
                IdAgendamento = (movimento.MovimentoItem as MovimentoItemTransferenciaTED)?.IdAgendamento ?? 0,
                Nsu = (movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra)?.Nsu,
                NumeroDocumento = movimento.NumeroDocumento, DataPagamento = movimento.DataPagamento, ValorPagamento = movimento.ValorPagamento, Unidade = unidade
            };
        }

        private Movimento MovimentoDe(ReferenciaPagamento referencia)
        {
            if (referencia == null) throw new ArgumentNullException(nameof(referencia));
            if (referencia.IdOperacao == Guid.Empty) throw new ArgumentException("Referência sem IdOperacao.", nameof(referencia));
            if (referencia.Banco != _origem.Banco || referencia.ContaOrigem != ContaReferencia)
                throw new ArgumentException("Use a referência na mesma conta e banco da operação original.", nameof(referencia));
            if (string.IsNullOrWhiteSpace(referencia.IdentificadorBanco) &&
                !(referencia.Modalidade == ModalidadePagamento.Boleto && !string.IsNullOrWhiteSpace(referencia.IdentidadeConsulta)))
                throw new ArgumentException("A referência ainda não contém o identificador do pagamento. Reconcilie o envio antes de consultar.", nameof(referencia));
            MovimentoItem item;
            switch (referencia.Modalidade)
            {
                case ModalidadePagamento.Boleto: item = new MovimentoItemPagamentoTituloCodigoBarra(); break;
                case ModalidadePagamento.Convenio: item = new MovimentoItemPagamentoConvenioCodigoBarra { Nsu = referencia.Nsu }; break;
                case ModalidadePagamento.Ted: item = new MovimentoItemTransferenciaTED { IdAgendamento = referencia.IdAgendamento }; break;
                case ModalidadePagamento.Pix: item = new MovimentoItemTransferenciaPIX(); break;
                case ModalidadePagamento.PixQrCode: item = new MovimentoItemPagamentoTituloPIXQRCode(); break;
                default: throw new ArgumentException("Modalidade de pagamento inválida.", nameof(referencia));
            }
            var movimento = new Movimento
            {
                NumeroDocumentoNoBanco = referencia.IdentificadorBanco, MovimentoItem = item,
                NumeroDocumento = referencia.NumeroDocumento, DataPagamento = referencia.DataPagamento, ValorPagamento = referencia.ValorPagamento
            };
            MetadadosOperacao.De(movimento).IdRequisicao = referencia.IdOperacao.ToString("D");
            MetadadosOperacao.De(movimento).IdentidadeConsulta = referencia.IdentidadeConsulta;
            return movimento;
        }
    }
}

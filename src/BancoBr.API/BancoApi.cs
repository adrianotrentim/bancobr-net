using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.Models;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.ContaCorrente;
using BancoBr.API.Sicoob.Pagamentos.Boletos;
using BancoBr.API.Sicoob.Pagamentos.Convenios;
using BancoBr.API.Sicoob.Pagamentos.Pix;
using BancoBr.API.Sicoob.Pagamentos.Ted;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API
{
    /// <summary>
    /// Ponto de entrada único da BancoBr.API: <see cref="Conectar(BancoEnum, string, string, CertificateSource, Uri)"/>
    /// devolve, numa chamada por banco/credenciais, um cliente já pronto para todas as operações
    /// (Pix, Boleto, Convênio, TED, Conta Corrente) — sem o chamador precisar escolher, por tipo,
    /// qual cliente instanciar. Os clientes internos (Pix, Boleto, Convênio, TED, Conta Corrente)
    /// são um detalhe de implementação, nunca expostos: todo método público desta classe já
    /// despacha pro cliente certo por baixo.
    ///
    /// Pra mandar um <see cref="Movimento"/> (Pix, Boleto, Convênio ou TED), consultar ou
    /// cancelar, use <see cref="EnviarAgendamentoAsync"/>/<see cref="ConsultarAgendamentoAsync"/>/<see cref="CancelarAgendamentoAsync"/>
    /// — eles despacham com base no tipo concreto de <see cref="Movimento.MovimentoItem"/>. Todo
    /// retorno da BancoBr.API é <see cref="Movimento"/> — inclusive consultas de lista que não
    /// partem de um único Movimento de entrada (DDA, extrato, pagamentos de convênio já
    /// realizados), que têm seu próprio método público abaixo e devolvem cada item com um
    /// <see cref="MovimentoItem"/> específico da operação (<see cref="MovimentoItemDDA"/>,
    /// <see cref="MovimentoItemExtratoTransacao"/>, <see cref="MovimentoItemPagamentoConvenioCodigoBarra"/>).
    /// </summary>
    public sealed class BancoApi
    {
        /// <summary>
        /// Fluxo padrão: autenticação OAuth2 client_credentials, renovada automaticamente por
        /// cada cliente interno (cada API do banco tem seus próprios scopes).
        /// </summary>
        /// <param name="tokenEndpointOverride">
        /// Só necessário em ambientes fora do padrão (ex.: sandbox com realm próprio); cada banco
        /// já tem um endpoint OAuth2 padrão embutido.
        /// </param>
        public static BancoApi Conectar(BancoEnum banco, string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpointOverride = null)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        new PagamentoBoletoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new PagamentoConvenioClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new PagamentoPixClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new TedClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new ExtratoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride));

                default:
                    throw new Exception($"Banco não implementado: {banco}!");
            }
        }

        /// <summary>
        /// Fluxo com token já emitido (ex.: portal de sandbox), pulando o fluxo OAuth2
        /// client_credentials — o mesmo token é reaproveitado pelos cinco clientes internos.
        /// </summary>
        public static BancoApi Conectar(BancoEnum banco, string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
        {
            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        new PagamentoBoletoClient(clientId, certificateSource, tokenProvider),
                        new PagamentoConvenioClient(clientId, certificateSource, tokenProvider),
                        new PagamentoPixClient(clientId, certificateSource, tokenProvider),
                        new TedClient(clientId, certificateSource, tokenProvider),
                        new ExtratoClient(clientId, certificateSource, tokenProvider));

                default:
                    throw new Exception($"Banco não implementado: {banco}!");
            }
        }

        private readonly PagamentoBoletoApiBase _boleto;
        private readonly PagamentoConvenioApiBase _convenio;
        private readonly PagamentoPixApiBase _pix;
        private readonly PagamentoTedApiBase _ted;
        private readonly ContaCorrenteApiBase _contaCorrente;

        private BancoApi(PagamentoBoletoApiBase boleto, PagamentoConvenioApiBase convenio, PagamentoPixApiBase pix, PagamentoTedApiBase ted, ContaCorrenteApiBase contaCorrente) =>
            (_boleto, _convenio, _pix, _ted, _contaCorrente) = (boleto, convenio, pix, ted, contaCorrente);

        /// <summary>
        /// Envia o movimento — consulta prévia (quando o banco exige) + pagamento, na mesma
        /// chamada. <paramref name="idempotencyKey"/> é opcional: para Boleto (que exige um GUID
        /// de lançamento por baixo) uma chave que não seja um GUID válido gera um novo; para TED
        /// é usada como veio, e uma nova é gerada quando omitida. <paramref name="unidade"/> só
        /// se aplica a Convênio (perfil "Parceiro Banco" multi-unidade). Pix por chave: se o
        /// movimento já foi iniciado por uma chamada prévia a <see cref="ConsultarAgendamentoAsync"/> (ex.:
        /// pra exibir o titular resolvido antes de confirmar), só confirma — não inicia de novo,
        /// o que geraria um EndToEndId novo e poderia trazer um titular diferente do aprovado.
        /// Mesma lógica para Boleto e Convênio: se já houver consulta prévia no movimento
        /// (<see cref="MovimentoItemPagamentoTituloCodigoBarra.IdentificadorConsulta"/> ou
        /// <see cref="MovimentoItemPagamentoConvenioCodigoBarra.Transacao"/>), paga com esses
        /// dados em vez de consultar de novo.
        /// </summary>
        public async Task<Movimento> EnviarAgendamentoAsync(Movimento movimento, Correntista origem = null, string idempotencyKey = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra itemBoleto:
                    if (!string.IsNullOrEmpty(itemBoleto.IdentificadorConsulta))
                    {
                        var chave = IdempotencyKey.New(origem.NumeroAgencia, origem.NumeroConta, ParseOuGerarGuid(idempotencyKey));
                        return await _boleto.PagarBoletoAsync(movimento, origem, chave, cancellationToken).ConfigureAwait(false);
                    }
                    return await _boleto.PagarBoletoComConsultaAsync(movimento, origem, ParseOuGerarGuid(idempotencyKey), cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoConvenioCodigoBarra itemConvenio:
                    if (itemConvenio.Transacao == null)
                        await _convenio.ConsultarCodigoBarrasAsync(movimento, origem, unidade: unidade, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return await _convenio.PagarConvenioAsync(movimento, origem, unidade, cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaTED _:
                    return await _ted.PagarTedAsync(movimento, origem, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaPIX _:
                    return string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco)
                        ? await _pix.PagarComIniciacaoAsync(movimento, cancellationToken).ConfigureAwait(false)
                        : await _pix.ConfirmarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    return await _pix.PagarViaQrCodeAsync(origem, movimento, cancellationToken).ConfigureAwait(false);

                default:
                    throw OperacaoNaoSuportada("envio", movimento);
            }
        }

        /// <summary>
        /// Consulta o movimento. Antes do envio (<see cref="Movimento.NumeroDocumentoNoBanco"/>
        /// ainda vazio), é a consulta prévia — pra exibir dados ao usuário antes de confirmar o
        /// pagamento (Boleto, Convênio) ou resolver a chave Pix. Depois do envio, é a consulta de
        /// situação/comprovante. Pix Copia e Cola não tem consulta prévia (o pagamento é direto),
        /// só de situação depois de pago.
        /// </summary>
        public Task<Movimento> ConsultarAgendamentoAsync(Movimento movimento, Correntista origem = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var jaEnviado = !string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco);

            switch (item)
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return jaEnviado
                        ? _boleto.ConsultarComprovantePorIdAsync(movimento, origem, cancellationToken)
                        : _boleto.ConsultarBoletoAsync(movimento, origem, cancellationToken);

                case MovimentoItemPagamentoConvenioCodigoBarra _:
                    return jaEnviado
                        ? _convenio.ConsultarComprovantePorNsuAsync(movimento, origem, cancellationToken)
                        : _convenio.ConsultarCodigoBarrasAsync(movimento, origem, unidade: unidade, cancellationToken: cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return _ted.ConsultarTedAsync(movimento, cancellationToken);

                case MovimentoItemTransferenciaPIX _:
                    return jaEnviado
                        ? _pix.ConsultarPagamentoAsync(movimento, cancellationToken)
                        : _pix.IniciarPagamentoAsync(movimento, cancellationToken);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    if (!jaEnviado)
                        throw new NotSupportedException("Pix Copia e Cola não tem consulta prévia — chame EnviarAgendamentoAsync diretamente; ConsultarAgendamentoAsync só se aplica depois do pagamento.");
                    return _pix.ConsultarPagamentoAsync(movimento, cancellationToken);

                default:
                    throw OperacaoNaoSuportada("consulta", movimento);
            }
        }

        /// <summary>
        /// Cancela o agendamento do movimento. Só Boleto e TED têm cancelamento na API do
        /// Sicoob — para Pix e Convênio, lança <see cref="NotSupportedException"/>.
        /// </summary>
        public Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, Correntista origem = null, string idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return _boleto.CancelarAgendamentoAsync(movimento, origem, cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return _ted.CancelarAgendamentoAsync(movimento, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken);

                default:
                    throw new NotSupportedException($"Cancelamento não é suportado para {movimento?.MovimentoItem?.GetType().Name ?? "null"} — a API do Sicoob não expõe esse endpoint para Pix/Convênio.");
            }
        }

        /// <summary>Consulta de lista: boletos DDA com vencimento/pagamento num intervalo — cada item vem como um <see cref="Movimento"/> com <see cref="MovimentoItemDDA"/>.</summary>
        public Task<IReadOnlyList<Movimento>> ConsultarDDAAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default)
            => _boleto.ConsultarBoletosDdaAsync(numeroConta, dataInicial, dataFinal, situacao, tipoData, cancellationToken);

        /// <summary>Consulta o extrato da conta no mês/ano informado — o primeiro item é o saldo do período (<see cref="MovimentoItemExtratoSaldo"/>), os demais são um por lançamento (<see cref="MovimentoItemExtratoTransacao"/>).</summary>
        public Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial = null, int? diaFinal = null, bool agruparCnab = false, CancellationToken cancellationToken = default)
            => _contaCorrente.ConsultarExtratoAsync(numeroContaCorrente, mes, ano, diaInicial, diaFinal, agruparCnab, cancellationToken);

        private static MovimentoItem ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            if (movimento.MovimentoItem == null) throw new ArgumentException("Movimento.MovimentoItem é obrigatório — é ele que determina qual operação bancária será chamada.", nameof(movimento));
            return movimento.MovimentoItem;
        }

        private static NotSupportedException OperacaoNaoSuportada(string operacao, Movimento movimento)
            => new NotSupportedException($"Não há {operacao} implementada para {movimento.MovimentoItem.GetType().Name}.");

        private static Guid ParseOuGerarGuid(string idempotencyKey)
            => Guid.TryParse(idempotencyKey, out var guid) ? guid : Guid.NewGuid();
    }
}

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
using SantanderExtratoClient = BancoBr.API.Santander.ContaCorrente.ExtratoClient;
using SantanderPagamentoBoletoClient =BancoBr.API.Santander.Pagamentos.Boletos.PagamentoBoletoClient;
using SantanderPagamentoConvenioClient = BancoBr.API.Santander.Pagamentos.Convenios.PagamentoConvenioClient;
using SantanderPagamentoPixClient = BancoBr.API.Santander.Pagamentos.Pix.PagamentoPixClient;
using SantanderPagamentoTedClient = BancoBr.API.Santander.Pagamentos.Ted.PagamentoTedClient;
using SantanderWorkspaceClient = BancoBr.API.Santander.Pagamentos.Workspaces.WorkspaceClient;

namespace BancoBr.API
{
    /// <summary>
    /// Ponto de entrada único da BancoBr.API: <see cref="Conectar(ConfiguracaoAPI, Uri)"/>
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
        /// <param name="configuracao">
        /// Credenciais, certificado digital (mTLS) e conta da API. O banco é o de
        /// <see cref="ConfiguracaoAPI.Correntista"/> (<see cref="Correntista.Banco"/>, código de compensação). <see cref="ConfiguracaoAPI.IdWorkspace"/> só se aplica ao
        /// Santander: id da workspace do Hub de Pagamentos usada em Pix, Boleto, Convênio, TED e
        /// DDA. Obtenha-o uma vez com <see cref="ObterOuCriarWorkspaceAsync"/> e guarde-o — assim
        /// as próximas conexões não consultam as workspaces do banco. Pode ser omitido na própria
        /// conexão usada para obter o id.
        /// </param>
        /// <param name="tokenEndpointOverride">
        /// Só necessário em ambientes fora do padrão (ex.: sandbox com realm próprio); cada banco
        /// já tem um endpoint OAuth2 padrão embutido.
        /// </param>
        public static BancoApi Conectar(ConfiguracaoAPI configuracao, Uri tokenEndpointOverride = null)
        {
            var banco = ObterBanco(configuracao);
            var clientId = configuracao.ClientId;
            var certificateSource = configuracao.CertificadoDigital;
            var clientSecret = configuracao.ClientSecret;
            var idWorkspace = configuracao.IdWorkspace;

            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        configuracao.Correntista,
                        new PagamentoBoletoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new PagamentoConvenioClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new PagamentoPixClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new TedClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new ExtratoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride));

                case BancoEnum.Santander:
                    return new BancoApi(
                        configuracao.Correntista,
                        new SantanderPagamentoBoletoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride, idWorkspace),
                        new SantanderPagamentoConvenioClient(clientId, clientSecret, certificateSource, tokenEndpointOverride, idWorkspace),
                        new SantanderPagamentoPixClient(clientId, clientSecret, certificateSource, tokenEndpointOverride, idWorkspace),
                        new SantanderPagamentoTedClient(clientId, clientSecret, certificateSource, tokenEndpointOverride, idWorkspace),
                        new SantanderExtratoClient(clientId, clientSecret, certificateSource, tokenEndpointOverride),
                        new SantanderWorkspaceClient(clientId, clientSecret, certificateSource, tokenEndpointOverride));

                default:
                    throw new Exception($"Banco não implementado: {configuracao.Correntista.Banco}!");
            }
        }

        /// <summary>
        /// Fluxo com token já emitido (ex.: portal de sandbox), pulando o fluxo OAuth2
        /// client_credentials — o mesmo token é reaproveitado pelos clientes internos.
        /// Só <see cref="ConfiguracaoAPI.ClientId"/>, <see cref="ConfiguracaoAPI.CertificadoDigital"/>
        /// e <see cref="ConfiguracaoAPI.IdWorkspace"/> (este só no Santander) são usados; <see cref="ConfiguracaoAPI.ClientSecret"/> é ignorado.
        /// </summary>
        public static BancoApi Conectar(ConfiguracaoAPI configuracao, IAccessTokenProvider tokenProvider)
        {
            var banco = ObterBanco(configuracao);
            var clientId = configuracao.ClientId;
            var certificateSource = configuracao.CertificadoDigital;
            var idWorkspace = configuracao.IdWorkspace;

            switch (banco)
            {
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        configuracao.Correntista,
                        new PagamentoBoletoClient(clientId, certificateSource, tokenProvider),
                        new PagamentoConvenioClient(clientId, certificateSource, tokenProvider),
                        new PagamentoPixClient(clientId, certificateSource, tokenProvider),
                        new TedClient(clientId, certificateSource, tokenProvider),
                        new ExtratoClient(clientId, certificateSource, tokenProvider));

                case BancoEnum.Santander:
                    return new BancoApi(
                        configuracao.Correntista,
                        new SantanderPagamentoBoletoClient(clientId, certificateSource, tokenProvider, idWorkspace),
                        new SantanderPagamentoConvenioClient(clientId, certificateSource, tokenProvider, idWorkspace),
                        new SantanderPagamentoPixClient(clientId, certificateSource, tokenProvider, idWorkspace),
                        new SantanderPagamentoTedClient(clientId, certificateSource, tokenProvider, idWorkspace),
                        new SantanderExtratoClient(clientId, certificateSource, tokenProvider),
                        new SantanderWorkspaceClient(clientId, certificateSource, tokenProvider));

                default:
                    throw new Exception($"Banco não implementado: {configuracao.Correntista.Banco}!");
            }
        }

        private readonly Correntista _origem;
        private readonly PagamentoBoletoApiBase _boleto;
        private readonly PagamentoConvenioApiBase _convenio;
        private readonly PagamentoPixApiBase _pix;
        private readonly PagamentoTedApiBase _ted;
        private readonly ContaCorrenteApiBase _contaCorrente;
        private readonly WorkspaceApiBase _workspace;

        private BancoApi(Correntista origem, PagamentoBoletoApiBase boleto, PagamentoConvenioApiBase convenio, PagamentoPixApiBase pix, PagamentoTedApiBase ted, ContaCorrenteApiBase contaCorrente, WorkspaceApiBase workspace = null) =>
            (_origem, _boleto, _convenio, _pix, _ted, _contaCorrente, _workspace) = (origem, boleto, convenio, pix, ted, contaCorrente, workspace);

        /// <summary>
        /// Só Santander. Consulta as workspaces do Hub de Pagamentos e devolve o id da workspace
        /// ativa que contém a conta pagadora (<see cref="ConfiguracaoAPI.Correntista"/>); se não existir, cria
        /// uma, e se existir com algum produto (Pix, boleto, DDA, convênio, TED) desligado,
        /// liga. Chame uma vez, guarde o id e informe-o em <see cref="Conectar(ConfiguracaoAPI, Uri)"/>
        /// nas próximas conexões — as operações de pagamento, efetivação e consulta usam esse id
        /// sem consultar o banco.
        /// </summary>
        public Task<string> ObterOuCriarWorkspaceAsync(CancellationToken cancellationToken = default)
            => Requer(_workspace).ObterOuCriarWorkspaceAsync(_origem, cancellationToken);

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
        public async Task<Movimento> EnviarAgendamentoAsync(Movimento movimento, string idempotencyKey = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra itemBoleto:
                    if (!string.IsNullOrEmpty(itemBoleto.IdentificadorConsulta))
                    {
                        var chave = IdempotencyKey.New(_origem.NumeroAgencia, _origem.NumeroConta, ParseOuGerarGuid(idempotencyKey));
                        return await Requer(_boleto).PagarBoletoAsync(movimento, _origem, chave, cancellationToken).ConfigureAwait(false);
                    }
                    return await Requer(_boleto).PagarBoletoComConsultaAsync(movimento, _origem, ParseOuGerarGuid(idempotencyKey), cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoConvenioCodigoBarra itemConvenio:
                    // Transacao (Sicoob) ou o id do pagamento em NumeroDocumentoNoBanco (Santander) indicam consulta prévia já feita.
                    if (itemConvenio.Transacao == null && string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco))
                        await Requer(_convenio).ConsultarCodigoBarrasAsync(movimento, _origem, unidade: unidade, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return await Requer(_convenio).PagarConvenioAsync(movimento, _origem, unidade, cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaTED _:
                    return await Requer(_ted).PagarTedAsync(movimento, _origem, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaPIX _:
                    return string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco)
                        ? await _pix.PagarComIniciacaoAsync(movimento, _origem, cancellationToken).ConfigureAwait(false)
                        : await _pix.ConfirmarPagamentoAsync(movimento, _origem, cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    return await _pix.PagarViaQrCodeAsync(_origem, movimento, cancellationToken).ConfigureAwait(false);

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
        public Task<Movimento> ConsultarAgendamentoAsync(Movimento movimento, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var jaEnviado = !string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco);

            switch (item)
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return jaEnviado
                        ? Requer(_boleto).ConsultarComprovantePorIdAsync(movimento, _origem, cancellationToken)
                        : Requer(_boleto).ConsultarBoletoAsync(movimento, _origem, cancellationToken);

                case MovimentoItemPagamentoConvenioCodigoBarra _:
                    return jaEnviado
                        ? Requer(_convenio).ConsultarComprovantePorNsuAsync(movimento, _origem, cancellationToken)
                        : Requer(_convenio).ConsultarCodigoBarrasAsync(movimento, _origem, unidade: unidade, cancellationToken: cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return Requer(_ted).ConsultarTedAsync(movimento, _origem, cancellationToken);

                case MovimentoItemTransferenciaPIX _:
                    return jaEnviado
                        ? _pix.ConsultarPagamentoAsync(movimento, _origem, cancellationToken)
                        : _pix.IniciarPagamentoAsync(movimento, _origem, cancellationToken);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    if (!jaEnviado)
                        throw new NotSupportedException("Pix Copia e Cola não tem consulta prévia — chame EnviarAgendamentoAsync diretamente; ConsultarAgendamentoAsync só se aplica depois do pagamento.");
                    return _pix.ConsultarPagamentoAsync(movimento, _origem, cancellationToken);

                default:
                    throw OperacaoNaoSuportada("consulta", movimento);
            }
        }

        /// <summary>
        /// Cancela o agendamento do movimento. Só Boleto e TED têm cancelamento na API do
        /// Sicoob — para Pix e Convênio, lança <see cref="NotSupportedException"/>.
        /// </summary>
        public Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return Requer(_boleto).CancelarAgendamentoAsync(movimento, _origem, cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return Requer(_ted).CancelarAgendamentoAsync(movimento, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken);

                default:
                    throw new NotSupportedException($"Cancelamento não é suportado para {movimento?.MovimentoItem?.GetType().Name ?? "null"} — a API do Sicoob não expõe esse endpoint para Pix/Convênio.");
            }
        }

        /// <summary>
        /// Consulta de lista: boletos DDA com vencimento/pagamento num intervalo — cada item vem
        /// como um <see cref="Movimento"/> com <see cref="MovimentoItemDDA"/>. No Santander a
        /// conta é localizada por <see cref="ConfiguracaoAPI.Correntista"/> (agência/conta/DV), que é obrigatório,
        /// e só há filtro por vencimento (<see cref="TipoDataConsultaEnum.Vencimento"/>).
        /// </summary>
        public Task<IReadOnlyList<Movimento>> ConsultarDDAAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default)
            => Requer(_boleto).ConsultarBoletosDdaAsync(numeroConta, dataInicial, dataFinal, situacao, tipoData, _origem, cancellationToken);

        /// <summary>
        /// Consulta o extrato da conta no mês/ano informado. No Sicoob, o primeiro item é o saldo
        /// do período (<see cref="MovimentoItemExtratoSaldo"/>) e os demais são um por lançamento
        /// (<see cref="MovimentoItemExtratoTransacao"/>); o Santander devolve só os lançamentos.
        /// No Santander a conta é localizada por <see cref="ConfiguracaoAPI.Correntista"/> (agência/conta/DV),
        /// que é obrigatório; no Sicoob vale <paramref name="numeroContaCorrente"/>.
        /// <paramref name="agruparCnab"/> só se aplica ao Sicoob.
        /// </summary>
        public Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial = null, int? diaFinal = null, bool agruparCnab = false, CancellationToken cancellationToken = default)
            => Requer(_contaCorrente).ConsultarExtratoAsync(numeroContaCorrente, mes, ano, diaInicial, diaFinal, agruparCnab, cancellationToken, _origem);

        /// <summary>O banco vem do código em <see cref="Correntista.Banco"/> (mesmos valores de <see cref="BancoEnum"/>).</summary>
        private static BancoEnum ObterBanco(ConfiguracaoAPI configuracao)
        {
            if (configuracao == null) throw new ArgumentNullException(nameof(configuracao));
            if (configuracao.Correntista == null) throw new ArgumentException("ConfiguracaoAPI.Correntista é obrigatório — é ele que determina o banco (Correntista.Banco) e a conta de origem das operações.", nameof(configuracao));
            return (BancoEnum)configuracao.Correntista.Banco;
        }

        private static MovimentoItem ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            if (movimento.MovimentoItem == null) throw new ArgumentException("Movimento.MovimentoItem é obrigatório — é ele que determina qual operação bancária será chamada.", nameof(movimento));
            return movimento.MovimentoItem;
        }

        private static NotSupportedException OperacaoNaoSuportada(string operacao, Movimento movimento)
            => new NotSupportedException($"Não há {operacao} implementada para {movimento.MovimentoItem.GetType().Name}.");

        /// <summary>Bancos com API parcial deixam as demais operações nulas.</summary>
        private static T Requer<T>(T api) where T : class
            => api ?? throw new NotSupportedException("Esta operação ainda não está implementada para o banco conectado.");

        private static Guid ParseOuGerarGuid(string idempotencyKey)
            => Guid.TryParse(idempotencyKey, out var guid) ? guid : Guid.NewGuid();
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using BancoBr.API.BancoDoBrasil.Http;
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
using SantanderComprovanteClient = BancoBr.API.Santander.Pagamentos.Comprovantes.ComprovanteClient;
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
            var origem = configuracao.Correntista;
            var clientId = configuracao.ClientId;
            var certificateSource = configuracao.CertificadoDigital;
            var clientSecret = configuracao.ClientSecret;
            var idWorkspace = configuracao.IdWorkspace;

            switch (banco)
            {
                case BancoEnum.Sicredi:
                    return ConectarSicredi(configuracao, tokenEndpointOverride, null);
                case BancoEnum.BancoDoBrasil:
                    return ConectarBancoDoBrasil(configuracao, tokenEndpointOverride, null);
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        configuracao.Correntista,
                        new PagamentoBoletoClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride),
                        new PagamentoConvenioClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride),
                        new PagamentoPixClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride),
                        new TedClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride),
                        new ExtratoClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride));

                case BancoEnum.Santander:
                    return new BancoApi(
                        configuracao.Correntista,
                        new SantanderPagamentoBoletoClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride, idWorkspace),
                        new SantanderPagamentoConvenioClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride, idWorkspace),
                        new SantanderPagamentoPixClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride, idWorkspace),
                        new SantanderPagamentoTedClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride, idWorkspace),
                        new SantanderExtratoClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride),
                        new SantanderWorkspaceClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride),
                        new SantanderComprovanteClient(clientId, clientSecret, certificateSource, origem, tokenEndpointOverride, idWorkspace));

                default:
                    throw new Exception($"Banco não implementado: {configuracao.Correntista.Banco}!");
            }
        }

        /// <summary>
        /// Fluxo com token já emitido (ex.: portal de sandbox), pulando o fluxo OAuth2
        /// client_credentials — o mesmo token é reaproveitado pelos clientes internos.
        /// Usa certificado e configuração do banco (incluindo chave da aplicação, ambiente e contrato no BB).
        /// <see cref="ConfiguracaoAPI.ClientSecret"/> é ignorado.
        /// </summary>
        public static BancoApi Conectar(ConfiguracaoAPI configuracao, IAccessTokenProvider tokenProvider)
        {
            var banco = ObterBanco(configuracao);
            var origem = configuracao.Correntista;
            var clientId = configuracao.ClientId;
            var certificateSource = configuracao.CertificadoDigital;
            var idWorkspace = configuracao.IdWorkspace;

            switch (banco)
            {
                case BancoEnum.Sicredi:
                    return ConectarSicredi(configuracao, null, tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider)));
                case BancoEnum.BancoDoBrasil:
                    return ConectarBancoDoBrasil(configuracao, null, tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider)));
                case BancoEnum.Sicoob:
                    return new BancoApi(
                        configuracao.Correntista,
                        new PagamentoBoletoClient(clientId, certificateSource, tokenProvider, origem),
                        new PagamentoConvenioClient(clientId, certificateSource, tokenProvider, origem),
                        new PagamentoPixClient(clientId, certificateSource, tokenProvider, origem),
                        new TedClient(clientId, certificateSource, tokenProvider, origem),
                        new ExtratoClient(clientId, certificateSource, tokenProvider, origem));

                case BancoEnum.Santander:
                    return new BancoApi(
                        configuracao.Correntista,
                        new SantanderPagamentoBoletoClient(clientId, certificateSource, tokenProvider, origem, idWorkspace),
                        new SantanderPagamentoConvenioClient(clientId, certificateSource, tokenProvider, origem, idWorkspace),
                        new SantanderPagamentoPixClient(clientId, certificateSource, tokenProvider, origem, idWorkspace),
                        new SantanderPagamentoTedClient(clientId, certificateSource, tokenProvider, origem, idWorkspace),
                        new SantanderExtratoClient(clientId, certificateSource, tokenProvider, origem),
                        new SantanderWorkspaceClient(clientId, certificateSource, tokenProvider, origem),
                        new SantanderComprovanteClient(clientId, certificateSource, tokenProvider, origem, idWorkspace));

                default:
                    throw new Exception($"Banco não implementado: {configuracao.Correntista.Banco}!");
            }
        }

        internal static BancoApi ConectarSicredi(ConfiguracaoAPI config, Uri endpoint, IAccessTokenProvider tokens, HttpClient httpClient = null)
        {
            if (config.CertificadoDigital == null && httpClient == null) throw new ArgumentException("Informe o certificado mTLS do Sicredi.");
            var http = httpClient ?? Sicredi.SicrediHttpGateway.CriarHttpClient(config.CertificadoDigital);
            var raiz = "https://mtls-api-parceiro.sicredi.com.br/" + (config.AmbienteHomologacao ? "sb/" : "");
            Func<string[], Sicredi.SicrediHttpGateway> gateway = scopes => new Sicredi.SicrediHttpGateway(http,
                tokens ?? new OAuthTokenProvider(http, new OAuthTokenProviderOptions
                {
                    TokenEndpoint = endpoint ?? new Uri(raiz + "thirdparty/auth/token"),
                    ClientId = config.ClientId, ClientSecret = config.ClientSecret, Scopes = scopes
                }), config.Correntista, config.AmbienteHomologacao);
            var boleto = new Sicredi.PagamentosClient(gateway(new[] { "multipag.boleto.pagar", "multipag.boleto.consultar" }));
            var convenio = new Sicredi.PagamentosClient(gateway(new[] { "multipag.tributos.pagar", "multipag.tributos.consultar" }));
            var pix = new Sicredi.PagamentosClient(gateway(new[] { "multipag.pix.pagar", "multipag.pix.consultar" }));
            var multipag = new Sicredi.MultipagClient(boleto, convenio, pix);
            return new BancoApi(config.Correntista,
                new Sicredi.PagamentoBoletoClient(boleto, new Sicredi.DdaClient(gateway(new[] { "multipag.dda.consultar" }))),
                new Sicredi.PagamentoConvenioClient(convenio), new Sicredi.PagamentoPixClient(pix), null,
                new Sicredi.ExtratoClient(gateway(new[] { "contacorrente.extratos.consultar" })),
                saldo: new Sicredi.ExtratoClient(gateway(new[] { "contacorrente.saldo.consultar" })),
                pagamentoDireto: multipag, comprovanteBinario: multipag);
        }

        /// <summary>Consulta o saldo atual da conta vinculada às credenciais, quando suportado pelo banco.</summary>
        public Task<Movimento> ConsultarSaldoAsync(CancellationToken cancellationToken = default)
            => Requer(_saldo).ConsultarSaldoAsync(cancellationToken);

        /// <summary>Baixa o comprovante em PDF binário, quando suportado pelo banco. No Sicredi, disponível após SUCESSO.</summary>
        public Task<byte[]> BaixarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            ExtrairItem(movimento);
            return Requer(_comprovanteBinario).BaixarComprovanteAsync(movimento, cancellationToken);
        }

        private static BancoApi ConectarBancoDoBrasil(ConfiguracaoAPI configuracao, Uri tokenEndpoint, IAccessTokenProvider tokenProvider)
        {
            if (string.IsNullOrWhiteSpace(configuracao.ChaveAplicacao))
                throw new ArgumentException("Informe ConfiguracaoAPI.ChaveAplicacao para o Banco do Brasil.", nameof(configuracao));
            if (configuracao.CertificadoDigital == null)
                throw new ArgumentException("Informe o certificado mTLS do Banco do Brasil.", nameof(configuracao));
            var endpoint = tokenEndpoint ?? new Uri(configuracao.AmbienteHomologacao
                ? "https://oauth.hm.bb.com.br/oauth/token" : "https://oauth.bb.com.br/oauth/token");
            var pagamentosToken = tokenProvider ?? new OAuthTokenProvider(new HttpClient(), new OAuthTokenProviderOptions
            {
                TokenEndpoint = endpoint, ClientId = configuracao.ClientId, ClientSecret = configuracao.ClientSecret,
                UseBasicAuthentication = true,
                Scopes = new[] { "pagamentos-lote.boletos-requisicao", "pagamentos-lote.boletos-info",
                    "pagamentos-lote.pagamentos-info",
                    "pagamentos-lote.guias-codigo-barras-requisicao", "pagamentos-lote.guias-codigo-barras-info",
                    "pagamentos-lote.transferencias-requisicao", "pagamentos-lote.transferencias-info",
                    "pagamentos-lote.transferencias-pix-requisicao", "pagamentos-lote.pix-info",
                    "pagamentos-lote.cancelar-requisicao" }
            });
            var extratoToken = tokenProvider ?? new OAuthTokenProvider(new HttpClient(), new OAuthTokenProviderOptions
            {
                TokenEndpoint = endpoint, ClientId = configuracao.ClientId, ClientSecret = configuracao.ClientSecret,
                UseBasicAuthentication = true, Scopes = new[] { "extrato-info" }
            });
            var http = BancoDoBrasilHttpGateway.BuildHttpClient(configuracao.CertificadoDigital);
            var url = new Uri(configuracao.AmbienteHomologacao
                ? "https://pagamentos-lote.mtls.api.hm.bb.com.br/v1/" : "https://pagamentos-lote.mtls.api.bb.com.br/v1/");
            return new BancoApi(configuracao.Correntista,
                new BancoDoBrasil.Pagamentos.PagamentoBoletoClient(http, pagamentosToken, configuracao.ChaveAplicacao, configuracao.Correntista, url),
                new BancoDoBrasil.Pagamentos.PagamentoConvenioClient(http, pagamentosToken, configuracao.ChaveAplicacao, configuracao.Correntista, url),
                new BancoDoBrasil.Pagamentos.PagamentoPixClient(http, pagamentosToken, configuracao.ChaveAplicacao, configuracao.Correntista, url),
                new BancoDoBrasil.Pagamentos.PagamentoTedClient(http, pagamentosToken, configuracao.ChaveAplicacao, configuracao.Correntista, url),
                new BancoDoBrasil.Extrato.ExtratoClient(configuracao, extratoToken));
        }

        private readonly Correntista _origem;
        private readonly PagamentoBoletoApiBase _boleto;
        private readonly PagamentoConvenioApiBase _convenio;
        private readonly PagamentoPixApiBase _pix;
        private readonly PagamentoTedApiBase _ted;
        private readonly ContaCorrenteApiBase _contaCorrente;
        private readonly WorkspaceApiBase _workspace;
        private readonly ComprovanteApiBase _comprovante;
        private readonly ISaldoApi _saldo;
        private readonly IPagamentoDiretoApi _pagamentoDireto;
        private readonly IComprovanteBinarioApi _comprovanteBinario;

        private BancoApi(Correntista origem, PagamentoBoletoApiBase boleto, PagamentoConvenioApiBase convenio, PagamentoPixApiBase pix, PagamentoTedApiBase ted, ContaCorrenteApiBase contaCorrente, WorkspaceApiBase workspace = null, ComprovanteApiBase comprovante = null,
            ISaldoApi saldo = null, IPagamentoDiretoApi pagamentoDireto = null, IComprovanteBinarioApi comprovanteBinario = null)
        {
            (_origem, _boleto, _convenio, _pix, _ted, _contaCorrente, _workspace, _comprovante) = (origem, boleto, convenio, pix, ted, contaCorrente, workspace, comprovante);
            (_saldo, _pagamentoDireto, _comprovanteBinario) = (saldo, pagamentoDireto, comprovanteBinario);
        }

        /// <summary>
        /// Só Santander. Consulta as workspaces do Hub de Pagamentos e devolve o id da workspace
        /// ativa que contém a conta pagadora (<see cref="ConfiguracaoAPI.Correntista"/>); se não existir, cria
        /// uma, e se existir com algum produto (Pix, boleto, DDA, convênio, TED) desligado,
        /// liga. Chame uma vez, guarde o id e informe-o em <see cref="Conectar(ConfiguracaoAPI, Uri)"/>
        /// nas próximas conexões — as operações de pagamento, efetivação e consulta usam esse id
        /// sem consultar o banco.
        /// </summary>
        public Task<string> ObterOuCriarWorkspaceAsync(CancellationToken cancellationToken = default)
            => Requer(_workspace).ObterOuCriarWorkspaceAsync(cancellationToken);

        /// <summary>
        /// No Sicredi, o envio é direto, sem consulta prévia: informe data, valor e NumeroDocumento.
        /// A identidade é preservada em IdRequisicaoBancoBr; a aceitação ainda depende de processamento/aprovação.
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
            if (_pagamentoDireto != null)
            {
                ExtrairItem(movimento);
                return await _pagamentoDireto.EnviarAsync(movimento, idempotencyKey, cancellationToken).ConfigureAwait(false);
            }
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra itemBoleto:
                    if (!string.IsNullOrEmpty(itemBoleto.IdentificadorConsulta))
                    {
                        var chave = IdempotencyKey.New(_origem.NumeroAgencia, _origem.NumeroConta, ParseOuGerarGuid(idempotencyKey));
                        return await Requer(_boleto).PagarBoletoAsync(movimento, chave, cancellationToken).ConfigureAwait(false);
                    }
                    return await Requer(_boleto).PagarBoletoComConsultaAsync(movimento, ParseOuGerarGuid(idempotencyKey), cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoConvenioCodigoBarra itemConvenio:
                    // Transacao (Sicoob) ou o id do pagamento em NumeroDocumentoNoBanco (Santander) indicam consulta prévia já feita.
                    if (Requer(_convenio).ExigeConsultaPrevia && itemConvenio.Transacao == null && string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco))
                        await Requer(_convenio).ConsultarCodigoBarrasAsync(movimento, unidade: unidade, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return await Requer(_convenio).PagarConvenioAsync(movimento, unidade, cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaTED _:
                    return await Requer(_ted).PagarTedAsync(movimento, idempotencyKey ?? Guid.NewGuid().ToString(), cancellationToken).ConfigureAwait(false);

                case MovimentoItemTransferenciaPIX _:
                    return string.IsNullOrEmpty(movimento.NumeroDocumentoNoBanco)
                        ? await Requer(_pix).PagarComIniciacaoAsync(movimento, cancellationToken).ConfigureAwait(false)
                        : await Requer(_pix).ConfirmarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    return await Requer(_pix).PagarViaQrCodeAsync(movimento, cancellationToken).ConfigureAwait(false);

                default:
                    throw OperacaoNaoSuportada("envio", movimento);
            }
        }

        /// <summary>
        /// No Sicredi, a consulta exige o idTransacao em NumeroDocumentoNoBanco; não existe consulta prévia por código/chave.
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
                        ? Requer(_boleto).ConsultarComprovantePorIdAsync(movimento, cancellationToken)
                        : Requer(_boleto).ConsultarBoletoAsync(movimento, cancellationToken);

                case MovimentoItemPagamentoConvenioCodigoBarra _:
                    return jaEnviado
                        ? Requer(_convenio).ConsultarComprovantePorNsuAsync(movimento, cancellationToken)
                        : Requer(_convenio).ConsultarCodigoBarrasAsync(movimento, unidade: unidade, cancellationToken: cancellationToken);

                case MovimentoItemTransferenciaTED _:
                    return Requer(_ted).ConsultarTedAsync(movimento, cancellationToken);

                case MovimentoItemTransferenciaPIX _:
                    return jaEnviado
                        ? Requer(_pix).ConsultarPagamentoAsync(movimento, cancellationToken)
                        : Requer(_pix).IniciarPagamentoAsync(movimento, cancellationToken);

                case MovimentoItemPagamentoTituloPIXQRCode _:
                    if (!jaEnviado)
                        throw new NotSupportedException("Pix Copia e Cola não tem consulta prévia — chame EnviarAgendamentoAsync diretamente; ConsultarAgendamentoAsync só se aplica depois do pagamento.");
                    return Requer(_pix).ConsultarPagamentoAsync(movimento, cancellationToken);

                default:
                    throw OperacaoNaoSuportada("consulta", movimento);
            }
        }

        /// <summary>
        /// Só Santander, para qualquer tipo de pagamento (Pix, TED, boleto e convênio). Obtém o link
        /// do comprovante (PDF) de um pagamento já efetivado — o id do pagamento vem de
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/> —
        /// e o devolve em <see cref="Movimento.LinkComprovanteBancoBr"/> (com a validade em
        /// <see cref="Movimento.ValidadeLinkComprovanteBancoBr"/>), junto da situação atualizada do pagamento.
        /// O banco pode demorar a gerar o arquivo: se ele não ficar pronto durante a chamada, o link
        /// volta vazio e basta chamar de novo mais tarde — a solicitação já feita é reaproveitada.
        /// </summary>
        public Task<Movimento> ConsultarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            ExtrairItem(movimento);

            return Requer(_comprovante).ConsultarComprovanteAsync(movimento, cancellationToken);
        }

        /// <summary>
        /// No Sicredi, cancela boleto, tributo com barras ou Pix agendado pelo idTransacao.
        /// Cancela o agendamento do movimento. Só Boleto e TED têm cancelamento na API do
        /// Sicoob — para Pix e Convênio, lança <see cref="NotSupportedException"/>.
        /// </summary>
        public Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey = null, CancellationToken cancellationToken = default)
        {
            if (_pagamentoDireto != null)
            {
                ExtrairItem(movimento);
                return _pagamentoDireto.CancelarAsync(movimento, cancellationToken);
            }
            switch (ExtrairItem(movimento))
            {
                case MovimentoItemPagamentoTituloCodigoBarra _:
                    return Requer(_boleto).CancelarAgendamentoAsync(movimento, cancellationToken);

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
            => Requer(_boleto).ConsultarBoletosDdaAsync(numeroConta, dataInicial, dataFinal, situacao, tipoData, cancellationToken);

        /// <summary>
        /// Consulta o extrato da conta no mês/ano informado. No Sicoob, o primeiro item é o saldo
        /// do período (<see cref="MovimentoItemExtratoSaldo"/>) e os demais são um por lançamento
        /// (<see cref="MovimentoItemExtratoTransacao"/>); o Santander devolve só os lançamentos.
        /// No Santander a conta é localizada por <see cref="ConfiguracaoAPI.Correntista"/> (agência/conta/DV),
        /// que é obrigatório; no Sicoob vale <paramref name="numeroContaCorrente"/>.
        /// <paramref name="agruparCnab"/> só se aplica ao Sicoob.
        /// </summary>
        public Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial = null, int? diaFinal = null, bool agruparCnab = false, CancellationToken cancellationToken = default)
            => Requer(_contaCorrente).ConsultarExtratoAsync(numeroContaCorrente, mes, ano, diaInicial, diaFinal, agruparCnab, cancellationToken);

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

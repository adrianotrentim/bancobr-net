using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Http;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using BancoBr.API.Santander.Pagamentos.Ted.Models;
using BancoBr.API.Sicoob.Pagamentos.Ted;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Pagamentos.Ted
{
    /// <summary>
    /// Cliente para a API "TED" (transfer) do Hub de Pagamentos do Santander
    /// (management_payments_partners v1). Como no Pix, todo pagamento tem dois passos: POST
    /// (iniciação, valida os dados e devolve READY_TO_PAY) e PATCH (efetivação, enviando
    /// AUTHORIZED). Só a câmara STR0008 (TED comum) é suportada. Não há cancelamento: a API do
    /// Santander não expõe esse endpoint.
    ///
    /// A chamada acontece dentro de uma workspace do Hub de Pagamentos, cujo id vem de
    /// <c>BancoApi.Conectar</c> — a workspace precisa ter transferências bancárias
    /// (<c>bankTransferPaymentsActive</c>) habilitadas, o que <c>ObterOuCriarWorkspaceAsync</c> garante.
    /// O id do pagamento (UUID do POST) fica em <see cref="Movimento.NumeroDocumentoNoBanco"/> e
    /// em <see cref="MovimentoItemTransferenciaTED.NumeroControleIF"/>.
    /// </summary>
    internal class PagamentoTedClient : PagamentoTedApiBase
    {
        public static readonly Uri BaseUrl = SantanderHttpGateway.PagamentosBaseUrl;

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly string _idWorkspace;

        internal PagamentoTedClient(string clientId, string clientSecret, CertificateSource certificateSource, Correntista origem, Uri tokenEndpoint = null, string idWorkspace = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint), origem, idWorkspace)
        {
        }

        internal PagamentoTedClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider, Correntista origem, string idWorkspace = null)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl, origem, idWorkspace)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public PagamentoTedClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl, Correntista origem, string idWorkspace = null)
            : base(httpClient, origem)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
            _idWorkspace = idWorkspace;
        }

        #region ::. Operações .::

        /// <summary>
        /// Inicia (POST) e, se o banco aceitou, efetiva (PATCH) a TED. <paramref name="idempotencyKey"/>
        /// vira o <c>id</c> do pagamento quando é um GUID — reenviar a mesma chave não duplica a TED.
        /// </summary>
        internal override async Task<Movimento> PagarTedAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaTED;
            if (item == null)
                throw new InvalidOperationException("PagarTedAsync espera um MovimentoItemTransferenciaTED em Movimento.MovimentoItem.");

            var workspace = ResolverWorkspace();

            var wireRequest = MontarRequisicao(movimento, item, workspace.Conta);
            if (Guid.TryParse(idempotencyKey, out var id))
                wireRequest.Id = id.ToString();

            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/transfer";
            var dto = await _gateway.SendAsync<RespostaTedSantander>(HttpMethod.Post, url, wireRequest, cancellationToken).ConfigureAwait(false);
            AplicarResposta(movimento, item, dto);

            if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            // A conta é enviada explicitamente: se a workspace foi achada por uma conta adicional,
            // sem isso o banco debitaria a conta principal.
            var confirmacao = new RequisicaoConfirmacaoTed { SourceAccount = ContaOrigem(workspace.Conta) };
            dto = await _gateway.SendAsync<RespostaTedSantander>(new HttpMethod("PATCH"), $"{url}/{movimento.NumeroDocumentoNoBanco}", confirmacao, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto);
        }

        internal override async Task<Movimento> ConsultarTedAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar uma TED, o movimento precisa ter o id do pagamento (devolvido por PagarTedAsync) em NumeroDocumentoNoBanco.");

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaTED;
            if (item == null)
                throw new InvalidOperationException("ConsultarTedAsync espera um MovimentoItemTransferenciaTED em Movimento.MovimentoItem.");

            var workspace = ResolverWorkspace();
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/transfer/{movimento.NumeroDocumentoNoBanco}";
            var dto = await _gateway.SendAsync<RespostaTedSantander>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto);
        }

        internal override Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de TED do Santander não expõe cancelamento.");

        #endregion

        #region ::. Workspace .::

        /// <summary>
        /// A workspace informada em <c>BancoApi.Conectar</c> (obtida por <c>ObterOuCriarWorkspaceAsync</c>)
        /// e a conta de débito da conta pagadora — sem consultar o banco.
        /// </summary>
        private (string WorkspaceId, ContaDebito Conta) ResolverWorkspace() =>
            SantanderHttpGateway.UsarWorkspace(_idWorkspace, Origem, "TED");

        #endregion

        #region ::. Mapeamento Movimento <-> Santander .::

        private static TedContaOrigem ContaOrigem(ContaDebito conta) =>
            new TedContaOrigem { BranchCode = conta.Branch, AccountNumber = conta.Number };

        private static RequisicaoTedSantander MontarRequisicao(Movimento movimento, MovimentoItemTransferenciaTED item, ContaDebito contaOrigem)
        {
            var favorecido = movimento.Favorecido;
            if (favorecido == null || string.IsNullOrWhiteSpace(favorecido.Nome) || string.IsNullOrWhiteSpace(favorecido.CPF_CNPJ))
                throw new InvalidOperationException("A TED exige o nome e o CPF/CNPJ do favorecido (Movimento.Favorecido).");

            if (item.Banco == 0 || item.NumeroAgencia == 0 || item.NumeroConta == 0)
                throw new InvalidOperationException("A TED exige banco, agência e conta do favorecido no movimento.");

            return new RequisicaoTedSantander
            {
                SourceAccount = ContaOrigem(contaOrigem),
                DestinationAccount = new TedContaDestino
                {
                    BankCode = item.Banco.ToString("D3"),
                    // O ISPB só é exigido para conta de pagamento.
                    IspbCode = item.TipoConta == TipoContaEnum.ContaPagamento ? TabelaIspbPorCompe.ObterIspb(item.Banco) : null,
                    BranchCode = item.NumeroAgencia.ToString("D4"),
                    AccountNumber = $"{item.NumeroConta}{item.DVConta}",
                    TypeAccount = MapTipoConta(item.TipoConta),
                    LegalEntityIdentifier = favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ ? "CNPJ" : "CPF",
                    DocumentIdentifierNumber = favorecido.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                    Name = favorecido.Nome,
                    Purpose = MapFinalidade(item.CodigoFinalidadeTED),
                },
                TransferHistory = item.Historico,
                TransferValue = movimento.ValorPagamento,
            };
        }

        private static string MapTipoConta(TipoContaEnum tipo)
        {
            switch (tipo)
            {
                case TipoContaEnum.ContaPoupanca:
                    return "PP";

                case TipoContaEnum.ContaPagamento:
                    return "PG";

                default:
                    return "CC";
            }
        }

        /// <summary>
        /// A API pede a finalidade por extenso. Só PAGAMENTO_DE_IMPOSTOS_TRIBUTOS_E_TAXAS aparece
        /// na documentação; os demais nomes seguem o mesmo padrão (descrição da tabela de
        /// finalidades em maiúsculas, sem acento, separada por "_") e devem ser validados no
        /// sandbox antes do uso em produção.
        /// </summary>
        private static string MapFinalidade(FinalidadeTEDEnum finalidade)
        {
            switch (finalidade)
            {
                case FinalidadeTEDEnum.PagamentoImpostosTributosTaxas:
                    return "PAGAMENTO_DE_IMPOSTOS_TRIBUTOS_E_TAXAS";

                case FinalidadeTEDEnum.PagamentoConcessionariaServicoPublico:
                    return "PAGAMENTO_A_CONCESSIONARIAS_DE_SERVICO_PUBLICO";

                case FinalidadeTEDEnum.PagamentoDividendos:
                    return "PAGAMENTOS_DE_DIVIDENDOS";

                case FinalidadeTEDEnum.PagamentoSalarios:
                    return "PAGAMENTO_DE_SALARIOS";

                case FinalidadeTEDEnum.PagamentoFornecedores:
                    return "PAGAMENTO_DE_FORNECEDORES";

                case FinalidadeTEDEnum.PagamentoHonorarios:
                    return "PAGAMENTO_DE_HONORARIOS";

                case FinalidadeTEDEnum.PagamentoAluguel:
                    return "PAGAMENTO_DE_ALUGUEIS_E_TAXAS_DE_CONDOMINIO";

                case FinalidadeTEDEnum.PagamentoDuplicatasTitulos:
                    return "PAGAMENTO_DE_DUPLICATAS_E_TITULOS";

                case FinalidadeTEDEnum.PagamentoMensalidadeEscolar:
                    return "PAGAMENTO_DE_MENSALIDADE_ESCOLAR";

                case FinalidadeTEDEnum.CreditoEmConta:
                    return "CREDITO_EM_CONTA";

                case FinalidadeTEDEnum.PagamentoCorretoras:
                    return "PAGAMENTO_A_CORRETORAS";

                case FinalidadeTEDEnum.PensaoALimenticia:
                    return "PENSAO_ALIMENTICIA";

                default:
                    throw new InvalidOperationException("A TED do Santander exige a finalidade (CodigoFinalidadeTED) no movimento.");
            }
        }

        private static Movimento AplicarResposta(Movimento movimento, MovimentoItemTransferenciaTED item, RespostaTedSantander dto)
        {
            if (dto == null)
                return movimento;

            if (!string.IsNullOrWhiteSpace(dto.Id))
            {
                movimento.NumeroDocumentoNoBanco = dto.Id;
                item.NumeroControleIF = dto.Id;
            }

            movimento.SituacaoBancoBr = SantanderHttpGateway.MapStatusParaSituacao(dto.Status);
            movimento.DetalheRejeicaoBancoBr = dto.RejectReason;
            item.Situacao = dto.Status;
            item.MensagemErro = dto.RejectReason;
            item.Agendado = movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Agendado;

            if (dto.TransferValue != 0)
                movimento.ValorPagamento = dto.TransferValue;

            if (TryParseData(dto.PaymentDate, out var dataPagamento))
                movimento.DataPagamento = dataPagamento;

            if (TryParseData(dto.Transaction?.Date, out var dataTransacao))
                movimento.HorarioBancoBr = dataTransacao;

            if (!string.IsNullOrWhiteSpace(dto.Transaction?.Code))
                item.CodigoAutenticacao = dto.Transaction.Code;

            if (!string.IsNullOrWhiteSpace(dto.TransferHistory))
                item.Historico = dto.TransferHistory;

            if (int.TryParse(dto.DestinationAccount?.BankCode, out var bancoFavorecido))
                item.NumeroBancoFavorecido = bancoFavorecido;

            return movimento;
        }

        private static bool TryParseData(string texto, out DateTime data) =>
            DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out data) && data != default(DateTime);

        #endregion
    }
}

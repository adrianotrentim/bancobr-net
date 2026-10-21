using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Core.Http;
using BancoBr.API.Santander.Http;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Pagamentos.Pix
{
    /// <summary>
    /// Cliente para a API "Pix" do Hub de Pagamentos do Santander (management_payments_partners
    /// v1) — transferência por chave DICT e pagamento de QR Code. Todo pagamento tem dois
    /// passos: POST (iniciação, valida a chave/QR e devolve READY_TO_PAY) e PATCH (efetivação,
    /// enviando AUTHORIZED). Transferência por agência e conta e webhook não são implementados.
    ///
    /// Toda chamada acontece dentro de uma workspace do Hub de Pagamentos, cujo id vem de
    /// <c>BancoApi.Conectar</c> (obtido uma vez por <c>ObterOuCriarWorkspaceAsync</c>).
    ///
    /// Assim como no Sicoob, esta classe é o limite de mapeamento entre o contrato público
    /// (<see cref="Movimento"/>/<see cref="MovimentoItem"/>) e os DTOs de wire
    /// (<c>Pix.Models.*</c>). O identificador do pagamento no banco (UUID do POST) é guardado
    /// em <see cref="Movimento.NumeroDocumentoNoBanco"/> — o EndToEndId só existe depois da
    /// efetivação e não é exposto.
    /// </summary>
    internal class PagamentoPixClient : PagamentoPixApiBase
    {
        public static readonly Uri BaseUrl = SantanderHttpGateway.PagamentosBaseUrl;

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly string _idWorkspace;

        internal PagamentoPixClient(string clientId, string clientSecret, CertificateSource certificateSource, Correntista origem, Uri tokenEndpoint = null, string idWorkspace = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint), origem, idWorkspace)
        {
        }

        internal PagamentoPixClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider, Correntista origem, string idWorkspace = null)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl, origem, idWorkspace)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public PagamentoPixClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl, Correntista origem, string idWorkspace = null)
            : base(httpClient, origem)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            _baseUrl = ComBarraFinal(baseUrl);
            _idWorkspace = idWorkspace;
        }

        private static string ComBarraFinal(Uri uri)
        {
            var texto = uri.ToString();
            return texto.EndsWith("/") ? texto : texto + "/";
        }

        #region ::. Operações .::

        internal override async Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var dto = await ObterPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, dto);
        }

        private async Task<PagamentoPixResponse> ObterPagamentoAsync(Movimento movimento, CancellationToken cancellationToken)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar um pagamento Pix, o movimento precisa ter o id do pagamento em NumeroDocumentoNoBanco.");

            var workspace = ResolverWorkspace();
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/pix_payments/{movimento.NumeroDocumentoNoBanco}";

            return await _gateway.SendAsync<PagamentoPixResponse>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<Movimento> IniciarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
            if (item == null)
                throw new InvalidOperationException("IniciarPagamentoAsync espera um MovimentoItemTransferenciaPIX em Movimento.MovimentoItem.");

            var wireRequest = new RequisicaoPagamentoPix
            {
                RemittanceInformation = movimento.NumeroDocumento,
                PaymentValue = movimento.ValorPagamento,
            };

            // Sem chave Pix, a transferência é para a agência e conta do beneficiário.
            if (string.IsNullOrWhiteSpace(item.ChavePIX))
            {
                wireRequest.Beneficiary = MontarBeneficiario(movimento, item);
            }
            else
            {
                wireRequest.DictCodeType = MapTipoChave(item);
                wireRequest.DictCode = item.ChavePIX;
            }

            return await IniciarAsync(movimento, wireRequest, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para confirmar um pagamento Pix, o movimento precisa ter o id do pagamento (devolvido por IniciarPagamentoAsync) em NumeroDocumentoNoBanco.");

            var workspace = ResolverWorkspace();
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/pix_payments/{movimento.NumeroDocumentoNoBanco}";

            // A conta é enviada explicitamente: se a workspace foi achada por uma conta adicional,
            // sem isso o banco debitaria a conta principal.
            var confirmacao = new RequisicaoConfirmacaoPix { PaymentValue = movimento.ValorPagamento, DebitAccount = workspace.Conta };

            var dto = await _gateway.SendAsync<PagamentoPixResponse>(new HttpMethod("PATCH"), url, confirmacao, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, dto);
        }

        /// <summary>
        /// Diferente do Sicoob, o QR Code também passa por iniciação + efetivação: o POST valida o
        /// QR e devolve READY_TO_PAY, e o PATCH efetiva.
        /// </summary>
        internal override async Task<Movimento> PagarViaQrCodeAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoTituloPIXQRCode;
            if (item == null)
                throw new InvalidOperationException("PagarViaQrCodeAsync espera um MovimentoItemPagamentoTituloPIXQRCode em Movimento.MovimentoItem.");

            var wireRequest = new RequisicaoPagamentoPix
            {
                RemittanceInformation = movimento.NumeroDocumento,
                QrCode = item.QrCode,
                PaymentValue = movimento.ValorPagamento,
                PaymentDate = movimento.DataPagamento.Date > DateTime.Today ? movimento.DataPagamento : (DateTime?)null,
            };

            await IniciarAsync(movimento, wireRequest, cancellationToken).ConfigureAwait(false);
            if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            return await ConfirmarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<Movimento> PagarComIniciacaoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            await IniciarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);
            if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            return await ConfirmarPagamentoAsync(movimento, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Movimento> IniciarAsync(Movimento movimento, RequisicaoPagamentoPix wireRequest, CancellationToken cancellationToken)
        {
            var workspace = ResolverWorkspace();
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/pix_payments";

            var dto = await _gateway.SendAsync<PagamentoPixResponse>(HttpMethod.Post, url, wireRequest, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, dto);
        }

        #endregion

        #region ::. Workspace .::

        /// <summary>
        /// A workspace informada em <c>BancoApi.Conectar</c> (obtida por <c>ObterOuCriarWorkspaceAsync</c>)
        /// e a conta de débito da conta pagadora — sem consultar o banco.
        /// </summary>
        private (string WorkspaceId, ContaDebito Conta) ResolverWorkspace() =>
            SantanderHttpGateway.UsarWorkspace(_idWorkspace, Origem, "Pix");

        #endregion

        #region ::. Mapeamento de resposta .::

        private static string MapTipoChave(MovimentoItemTransferenciaPIX item)
        {
            switch (item.TipoChavePIX)
            {
                case FormaIniciacaoEnum.PIX_Telefone:
                    return "CELULAR";

                case FormaIniciacaoEnum.PIX_Email:
                    return "EMAIL";

                case FormaIniciacaoEnum.PIX_Aleatorio:
                    return "EVP";

                case FormaIniciacaoEnum.PIX_CPF_CNPJ:
                    return new string(item.ChavePIX.Where(char.IsDigit).ToArray()).Length > 11 ? "CNPJ" : "CPF";

                default:
                    throw new InvalidOperationException("O tipo da chave Pix (TipoChavePIX) não foi informado no movimento.");
            }
        }

        private static PixBeneficiario MontarBeneficiario(Movimento movimento, MovimentoItemTransferenciaPIX item)
        {
            if (item.NumeroAgencia == 0 || item.NumeroConta == 0)
                throw new InvalidOperationException("Informe a chave Pix, ou a agência e a conta do beneficiário, no movimento.");

            if (movimento.Favorecido == null || string.IsNullOrWhiteSpace(movimento.Favorecido.Nome) || string.IsNullOrWhiteSpace(movimento.Favorecido.CPF_CNPJ))
                throw new InvalidOperationException("O Pix por agência e conta exige o nome e o CPF/CNPJ do favorecido (Movimento.Favorecido).");

            var informouBanco = item.Banco != 0;
            var informouIspb = !string.IsNullOrWhiteSpace(item.Ispb);
            if (informouBanco == informouIspb)
                throw new InvalidOperationException("Informe o banco do beneficiário por código (Banco) ou por ISPB (Ispb) — um dos dois, nunca ambos.");

            string tipoConta;
            switch (item.TipoConta)
            {
                case TipoContaEnum.ContaPoupanca:
                    tipoConta = "CONTA_POUPANCA";
                    break;

                case TipoContaEnum.ContaPagamento:
                    tipoConta = "CONTA_PAGAMENTO";
                    break;

                default:
                    tipoConta = "CONTA_CORRENTE";
                    break;
            }

            return new PixBeneficiario
            {
                Name = movimento.Favorecido.Nome,
                DocumentType = movimento.Favorecido.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ ? "CNPJ" : "CPF",
                DocumentNumber = movimento.Favorecido.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                BankCode = informouBanco ? item.Banco.ToString("D3") : null,
                Ispb = informouIspb ? item.Ispb : null,
                Branch = item.NumeroAgencia.ToString("D4"),
                Number = $"{item.NumeroConta}{item.DVConta}",
                Type = tipoConta,
            };
        }

        private static Movimento AplicarResposta(Movimento movimento, PagamentoPixResponse dto)
        {
            if (dto == null)
                return movimento;

            if (!string.IsNullOrWhiteSpace(dto.Id))
                movimento.NumeroDocumentoNoBanco = dto.Id;

            movimento.SituacaoBancoBr = SantanderHttpGateway.MapStatusParaSituacao(dto.Status);
            movimento.DetalheRejeicaoBancoBr = dto.RejectReason;
            movimento.HorarioBancoBr = dto.Transaction?.Date;

            if (dto.PaymentValue != 0)
                movimento.ValorPagamento = dto.PaymentValue;

            var itemPix = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
            if (itemPix != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.DictCode))
                    itemPix.ChavePIX = dto.DictCode;

                if (!string.IsNullOrWhiteSpace(dto.DictCodeType))
                    itemPix.TipoChaveConfirmado = dto.DictCodeType;

                if (dto.Beneficiary != null)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Beneficiary.Name))
                        itemPix.TitularConfirmadoNome = dto.Beneficiary.Name;

                    if (!string.IsNullOrWhiteSpace(dto.Beneficiary.DocumentNumber))
                        itemPix.TitularConfirmadoDocumento = dto.Beneficiary.DocumentNumber;

                    if (!string.IsNullOrWhiteSpace(dto.Beneficiary.DocumentType))
                        itemPix.TitularConfirmadoTipo = dto.Beneficiary.DocumentType;
                }
            }

            var itemQrCode = movimento.MovimentoItem as MovimentoItemPagamentoTituloPIXQRCode;
            if (itemQrCode != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.QrCodeIdentification))
                    itemQrCode.TXID = dto.QrCodeIdentification;

                if (dto.NominalValue != 0)
                    itemQrCode.ValorOriginal = dto.NominalValue;

                if (dto.DueDate.HasValue)
                    itemQrCode.DataVencimento = dto.DueDate.Value;
            }

            return movimento;
        }

        #endregion
    }
}

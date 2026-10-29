using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Http;
using BancoBr.API.Santander.Pagamentos.Models;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Pagamentos.Comprovantes
{
    /// <summary>
    /// Cliente do comprovante (PDF) de pagamentos do Hub de Pagamentos do Santander, para
    /// qualquer modalidade (Pix, TED, boleto e convênio). O processo é o mesmo em todas: consulta
    /// o pagamento na workspace para obter o <c>transaction.code</c> e, com ele, usa a API
    /// consult_payment_receipts v2 para solicitar o arquivo, obter o link e baixar o PDF. O que muda por
    /// modalidade é só o recurso consultado (veja <see cref="Identificar"/>).
    ///
    /// O link é usado somente no download interno. Se o banco ainda não gerou o arquivo, retorna false
    /// e basta chamar de novo — a solicitação já feita é reaproveitada.
    /// </summary>
    internal class ComprovanteClient : ComprovanteApiBase
    {
        /// <summary>Quantas vezes o link é consultado antes de retornar false por arquivo ainda indisponível.</summary>
        private const int Tentativas = 5;

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly string _comprovantesBaseUrl;
        private readonly string _idWorkspace;
        private readonly TimeSpan _intervalo;

        internal ComprovanteClient(string clientId, string clientSecret, CertificateSource certificateSource, Correntista origem, Uri tokenEndpoint = null, string idWorkspace = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint), origem, idWorkspace)
        {
        }

        internal ComprovanteClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider, Correntista origem, string idWorkspace = null)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, SantanderHttpGateway.PagamentosBaseUrl, origem, idWorkspace)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public ComprovanteClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl, Correntista origem, string idWorkspace = null, Uri comprovantesBaseUrl = null, TimeSpan? intervaloComprovante = null)
            : base(httpClient, origem)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            _baseUrl = ComBarraFinal(baseUrl);
            _comprovantesBaseUrl = ComBarraFinal(comprovantesBaseUrl ?? SantanderHttpGateway.ComprovantesBaseUrl);
            _idWorkspace = idWorkspace;
            _intervalo = intervaloComprovante ?? TimeSpan.FromSeconds(2);
        }

        internal override async Task<bool> ConsultarComprovanteAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            movimento.ArquivoComprovantePDF = null;

            var (recurso, produto, id) = Identificar(movimento);
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Para consultar o comprovante, o movimento precisa ter o id do pagamento em NumeroDocumentoNoBanco.");

            var workspace = SantanderHttpGateway.UsarWorkspace(_idWorkspace, Origem, produto);
            var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/{recurso}/{id}";
            var pagamento = await _gateway.SendAsync<PagamentoTransacaoResponse>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);

            movimento.Situacao = SantanderHttpGateway.MapStatusParaSituacao(pagamento?.Status);
            if (movimento.Situacao != BancoBrSituacaoEnum.Efetivado) return false;

            var codigoTransacao = pagamento?.Transaction?.Code;
            if (string.IsNullOrWhiteSpace(codigoTransacao))
                return false;

            var comprovante = await _gateway.ObterComprovanteAsync(_comprovantesBaseUrl, codigoTransacao, Tentativas, _intervalo, cancellationToken).ConfigureAwait(false);
            if (comprovante == null) return false;

            var pdf = await _gateway.BaixarArquivoComprovanteAsync(comprovante.File.FileRepository.Location, cancellationToken).ConfigureAwait(false);
            movimento.ArquivoComprovantePDF = pdf;
            return true;
        }

        /// <summary>
        /// O recurso da workspace onde o pagamento é consultado, o nome do produto (usado na
        /// mensagem de workspace ausente) e o id do pagamento, conforme o tipo do movimento. No
        /// boleto, enquanto o id ainda não foi copiado para <see cref="Movimento.NumeroDocumentoNoBanco"/>,
        /// vale o de <see cref="MovimentoItemPagamentoTituloCodigoBarra.IdentificadorConsulta"/>.
        /// </summary>
        private static (string Recurso, string Produto, string Id) Identificar(Movimento movimento)
        {
            switch (movimento.MovimentoItem)
            {
                case MovimentoItemTransferenciaPIX _:
                case MovimentoItemPagamentoTituloPIXQRCode _:
                    return ("pix_payments", "Pix", movimento.NumeroDocumentoNoBanco);

                case MovimentoItemTransferenciaTED _:
                    return ("transfer", "TED", movimento.NumeroDocumentoNoBanco);

                case MovimentoItemPagamentoConvenioCodigoBarra _:
                    return ("barcode_payments", "Convênios", movimento.NumeroDocumentoNoBanco);

                case MovimentoItemPagamentoTituloCodigoBarra boleto:
                    return ("bank_slip_payments", "Boletos", !string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco) ? movimento.NumeroDocumentoNoBanco : boleto.IdentificadorConsulta);

                default:
                    throw new NotSupportedException($"A consulta de comprovante não é suportada para {movimento.MovimentoItem?.GetType().Name ?? "movimento sem item"}.");
            }
        }

        private static string ComBarraFinal(Uri uri)
        {
            var texto = uri.ToString();
            return texto.EndsWith("/") ? texto : texto + "/";
        }
    }
}

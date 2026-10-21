using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.Http;
using BancoBr.API.Santander.Pagamentos.Models;
using BancoBr.API.Santander.Pagamentos.Pix.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Pagamentos.Convenios
{
    /// <summary>
    /// Cliente para a API "Código de barras" (barcode_payments) do Hub de Pagamentos do
    /// Santander (management_payments_partners v1) — arrecadação de convênios, contas de consumo
    /// e tributos com código de barras (inicia com 8). Todo pagamento tem dois passos: POST
    /// (iniciação, valida o código e devolve READY_TO_PAY) e PATCH (efetivação, enviando
    /// AUTHORIZED e o pagador final).
    ///
    /// Assim como o Pix, a chamada acontece dentro de uma workspace do Hub de Pagamentos, cujo id
    /// vem de <c>BancoApi.Conectar</c> (obtido por <c>ObterOuCriarWorkspaceAsync</c>). O id do pagamento devolvido pelo POST
    /// fica em <see cref="Movimento.NumeroDocumentoNoBanco"/> — o Santander não tem NSU nem
    /// transação numérica como o Sicoob, então <see cref="MovimentoItemPagamentoConvenioCodigoBarra.Nsu"/>
    /// e <see cref="MovimentoItemPagamentoConvenioCodigoBarra.Transacao"/> não são preenchidos.
    /// Consultas de lista, conciliação e convênios habilitados não são suportadas.
    /// </summary>
    internal class PagamentoConvenioClient : PagamentoConvenioApiBase
    {
        private const string Recurso = "barcode_payments";

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly string _idWorkspace;

        internal PagamentoConvenioClient(string clientId, string clientSecret, CertificateSource certificateSource, Correntista origem, Uri tokenEndpoint = null, string idWorkspace = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint), origem, idWorkspace)
        {
        }

        internal PagamentoConvenioClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider, Correntista origem, string idWorkspace = null)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, SantanderHttpGateway.PagamentosBaseUrl, origem, idWorkspace)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public PagamentoConvenioClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl, Correntista origem, string idWorkspace = null)
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
        /// Inicia o pagamento (POST): valida o código de barras e traz o convênio e o valor.
        /// <paramref name="recebimentoViaCaixa"/> e <paramref name="unidade"/> não se aplicam ao Santander.
        /// </summary>
        internal override async Task<Movimento> ConsultarCodigoBarrasAsync(Movimento movimento, bool? recebimentoViaCaixa = null, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            if (string.IsNullOrWhiteSpace(item.CodigoBarra))
                throw new InvalidOperationException("Informe o código de barras (CodigoBarra) do convênio.");

            var workspace = ResolverWorkspace();
            var dto = await _gateway.IniciarPagamentoCodigoAsync(_baseUrl, Recurso, workspace.WorkspaceId, ParaLinhaDigitavel(item.CodigoBarra.JustNumbers()), movimento.DataPagamento, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto);
        }

        /// <summary>Efetiva o pagamento (PATCH) do código já iniciado, com o id guardado em <see cref="Movimento.NumeroDocumentoNoBanco"/>.</summary>
        internal override async Task<Movimento> PagarConvenioAsync(Movimento movimento, int unidade = 0, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para pagar o convênio, o movimento precisa ter o id do pagamento (devolvido por ConsultarCodigoBarrasAsync) em NumeroDocumentoNoBanco.");

            if (movimento.ValorPagamento <= 0)
                throw new InvalidOperationException("Informe o valor do pagamento (Movimento.ValorPagamento).");

            var workspace = ResolverWorkspace();
            var dto = await _gateway.EfetivarPagamentoCodigoAsync(_baseUrl, Recurso, workspace, movimento.NumeroDocumentoNoBanco, movimento.ValorPagamento, Origem, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto);
        }

        /// <summary>Consulta a situação do pagamento pelo id em <see cref="Movimento.NumeroDocumentoNoBanco"/> (o Santander não usa NSU).</summary>
        internal override async Task<Movimento> ConsultarComprovantePorNsuAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar o pagamento, o movimento precisa ter o id do pagamento em NumeroDocumentoNoBanco.");

            var workspace = ResolverWorkspace();
            var dto = await _gateway.ConsultarPagamentoCodigoAsync(_baseUrl, Recurso, workspace.WorkspaceId, movimento.NumeroDocumentoNoBanco, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto);
        }

        internal override Task<IReadOnlyList<Movimento>> ConsultarPagamentosAsync(string codigoBarras, long instituicao, DateTime dataMovimento, long? transacao = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A consulta de lista de pagamentos de convênio do Santander ainda não está implementada.");

        internal override Task<IReadOnlyList<ConciliacaoItem>> ConsultarConciliacoesAsync(long instituicao, DateTime dataMovimento, int? unidade = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A conciliação de convênios do Santander ainda não está implementada.");

        internal override Task<IReadOnlyList<ConvenioHabilitado>> ConsultarConveniosHabilitadosAsync(long transacao, long instituicao, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de código de barras do Santander não lista convênios habilitados.");

        #endregion

        #region ::. Workspace .::

        /// <summary>
        /// A workspace informada em <c>BancoApi.Conectar</c> (obtida por <c>ObterOuCriarWorkspaceAsync</c>)
        /// e a conta de débito da conta pagadora — sem consultar o banco.
        /// </summary>
        private (string WorkspaceId, ContaDebito Conta) ResolverWorkspace() =>
            SantanderHttpGateway.UsarWorkspace(_idWorkspace, Origem, "Convênios");

        #endregion

        #region ::. Código .::

        /// <summary>
        /// O Santander espera a linha digitável (48 dígitos, com o DV de cada bloco de 11). Quando
        /// vem o código de barras (44 dígitos), cada DV é recalculado: módulo 10 se o 3º dígito é
        /// 6 ou 7, módulo 11 se é 8 ou 9. Vai sem traços nem espaços, com o DV colado ao bloco de 11
        /// dígitos e um ponto entre os blocos (<c>858200000074.293703852623.930716262907.705041379220</c>).
        /// Qualquer outro tamanho segue como veio.
        /// </summary>
        private static string ParaLinhaDigitavel(string codigo)
        {
            if (codigo.Length == 48)
                return string.Join(".", Enumerable.Range(0, 4).Select(bloco => codigo.Substring(bloco * 12, 12)));

            if (codigo.Length != 44)
                return codigo;

            var modulo10 = codigo[2] == '6' || codigo[2] == '7';
            var blocos = new string[4];

            for (var bloco = 0; bloco < 4; bloco++)
            {
                var campo = codigo.Substring(bloco * 11, 11);
                blocos[bloco] = $"{campo}{(modulo10 ? DigitoModulo10(campo) : DigitoModulo11(campo))}";
            }

            return string.Join(".", blocos);
        }

        private static int DigitoModulo10(string campo)
        {
            var soma = 0;
            var peso = 2;

            for (var i = campo.Length - 1; i >= 0; i--)
            {
                var produto = (campo[i] - '0') * peso;
                soma += produto / 10 + produto % 10;
                peso = peso == 2 ? 1 : 2;
            }

            return (10 - soma % 10) % 10;
        }

        private static int DigitoModulo11(string campo)
        {
            var soma = 0;
            var peso = 2;

            for (var i = campo.Length - 1; i >= 0; i--)
            {
                soma += (campo[i] - '0') * peso;
                peso = peso == 9 ? 2 : peso + 1;
            }

            var digito = 11 - soma % 11;
            return digito >= 10 ? 0 : digito;
        }

        #endregion

        #region ::. Mapeamento de resposta .::

        private static MovimentoItemPagamentoConvenioCodigoBarra ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra;
            if (item == null)
                throw new InvalidOperationException("As operações de convênio esperam um MovimentoItemPagamentoConvenioCodigoBarra em Movimento.MovimentoItem.");

            return item;
        }

        private static Movimento AplicarResposta(Movimento movimento, MovimentoItemPagamentoConvenioCodigoBarra item, PagamentoCodigoResponse dto)
        {
            if (dto == null)
                return movimento;

            if (!string.IsNullOrWhiteSpace(dto.Id))
                movimento.NumeroDocumentoNoBanco = dto.Id;

            SantanderHttpGateway.AplicarStatusCodigo(movimento, dto);

            if (dto.TotalValue.HasValue)
                item.ValorDocumento = dto.TotalValue.Value;

            if (!string.IsNullOrWhiteSpace(dto.Concessionary?.Name))
                item.Convenio = dto.Concessionary.Name;

            if (!string.IsNullOrWhiteSpace(dto.Concessionary?.Code))
                item.CodigoConvenioFebraban = dto.Concessionary.Code;

            if (!string.IsNullOrWhiteSpace(dto.Transaction?.Code))
                item.Autenticacao = dto.Transaction.Code;

            return movimento;
        }

        #endregion
    }
}

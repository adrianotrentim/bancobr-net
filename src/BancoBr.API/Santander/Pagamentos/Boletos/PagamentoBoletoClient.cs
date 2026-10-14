using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.Pagamentos.Boletos
{
    /// <summary>
    /// Cliente para a API "Boletos" (bank_slip_payments) do Hub de Pagamentos do Santander
    /// (management_payments_partners v1) — boletos do Santander e de outros bancos. Todo
    /// pagamento tem dois passos: POST (iniciação, valida o boleto e devolve READY_TO_PAY com os
    /// valores atualizados) e PATCH (efetivação, enviando AUTHORIZED e o pagador final).
    ///
    /// Assim como o Pix, a chamada acontece dentro de uma workspace do Hub de Pagamentos, cujo id
    /// vem de <c>BancoApi.Conectar</c> (obtido por <c>ObterOuCriarWorkspaceAsync</c>). O id do pagamento devolvido pelo POST
    /// fica em <see cref="MovimentoItemPagamentoTituloCodigoBarra.IdentificadorConsulta"/> (é o
    /// que a efetivação exige de volta) e, depois de efetivado, também em
    /// <see cref="Movimento.NumeroDocumentoNoBanco"/>. A API não tem cancelamento, consulta de
    /// comprovante por idempotency key, então essas operações não são suportadas. O DDA
    /// (authorized_direct_debits) exige que a workspace tenha o DDA e o pagamento de boletos ligados.
    /// </summary>
    internal class PagamentoBoletoClient : PagamentoBoletoApiBase
    {
        private const string Recurso = "bank_slip_payments";

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly string _idWorkspace;

        internal PagamentoBoletoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null, string idWorkspace = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint), idWorkspace)
        {
        }

        internal PagamentoBoletoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider, string idWorkspace = null)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, SantanderHttpGateway.PagamentosBaseUrl, idWorkspace)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public PagamentoBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl, string idWorkspace = null)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
            _idWorkspace = idWorkspace;
        }

        #region ::. Operações .::

        /// <summary>
        /// Inicia o pagamento (POST): valida o boleto e traz valor, vencimento, pagador e
        /// beneficiário. Rejeições de negócio (boleto vencido, já pago...) chegam como erro do
        /// banco (<see cref="Core.Errors.BancoApiException"/>).
        /// </summary>
        internal override async Task<Movimento> ConsultarBoletoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            var codigo = !string.IsNullOrWhiteSpace(item.CodigoBarras) ? item.CodigoBarras : item.NumeroLinhaDigitavel;
            if (string.IsNullOrWhiteSpace(codigo))
                throw new InvalidOperationException("Informe o código de barras (CodigoBarras) ou a linha digitável (NumeroLinhaDigitavel) do boleto.");

            var workspace = ResolverWorkspace(origem);
            var dto = await _gateway.IniciarPagamentoCodigoAsync(_baseUrl, Recurso, workspace.WorkspaceId, codigo.JustNumbers(), movimento.DataPagamento, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto, iniciacao: true);
        }

        /// <summary>
        /// Efetiva o pagamento (PATCH) do boleto já iniciado, com o id guardado em
        /// <see cref="MovimentoItemPagamentoTituloCodigoBarra.IdentificadorConsulta"/>. A
        /// <paramref name="idempotencyKey"/> não é usada: o Santander deduplica pelo id do pagamento.
        /// </summary>
        internal override async Task<Movimento> PagarBoletoAsync(Movimento movimento, Correntista origem, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            var id = item.IdentificadorConsulta;
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Para pagar o boleto, o movimento precisa ter o id do pagamento (devolvido por ConsultarBoletoAsync) em IdentificadorConsulta.");

            if (movimento.ValorPagamento <= 0)
                throw new InvalidOperationException("Informe o valor do pagamento (Movimento.ValorPagamento).");

            var workspace = ResolverWorkspace(origem);
            var dto = await _gateway.EfetivarPagamentoCodigoAsync(_baseUrl, Recurso, workspace, id, movimento.ValorPagamento, origem, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto, iniciacao: false);
        }

        internal override async Task<Movimento> PagarBoletoComConsultaAsync(Movimento movimento, Correntista origem, Guid idLancamento, CancellationToken cancellationToken = default)
        {
            await ConsultarBoletoAsync(movimento, origem, cancellationToken).ConfigureAwait(false);
            if (movimento.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            return await PagarBoletoAsync(movimento, origem, idempotencyKey: null, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<(Movimento Movimento, Guid IdLancamento)> itens, Correntista origem, CancellationToken cancellationToken = default)
        {
            if (itens == null) throw new ArgumentNullException(nameof(itens));

            var resultados = new List<PagamentoBoletoLoteResultadoItem>();

            foreach (var item in itens)
            {
                try
                {
                    var movimento = await PagarBoletoComConsultaAsync(item.Movimento, origem, item.IdLancamento, cancellationToken).ConfigureAwait(false);

                    resultados.Add(PagamentoBoletoLoteResultadoItem.ComSucesso(movimento, item.IdLancamento));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    resultados.Add(PagamentoBoletoLoteResultadoItem.ComFalha(item.Movimento, item.IdLancamento, ex));
                }
            }

            return resultados;
        }

        internal override async Task<Movimento> ConsultarComprovantePorIdAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);

            var id = !string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco) ? movimento.NumeroDocumentoNoBanco : item.IdentificadorConsulta;
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Para consultar o pagamento, o movimento precisa ter o id do pagamento em NumeroDocumentoNoBanco.");

            var workspace = ResolverWorkspace(origem);
            var dto = await _gateway.ConsultarPagamentoCodigoAsync(_baseUrl, Recurso, workspace.WorkspaceId, id, cancellationToken).ConfigureAwait(false);

            return AplicarResposta(movimento, item, dto, iniciacao: false);
        }

        internal override Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de boletos do Santander não tem cancelamento de agendamento.");

        internal override Task<Movimento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de boletos do Santander não consulta pagamentos por idempotency key — use o id do pagamento.");

        /// <summary>
        /// Consulta os boletos DDA (authorized_direct_debits) da workspace informada em
        /// <c>Conectar</c>, paginando de 200 em 200. O Santander só filtra por
        /// vencimento, então <paramref name="dataInicial"/>/<paramref name="dataFinal"/> são o
        /// intervalo de vencimento e outros <paramref name="tipoData"/> não são suportados; o
        /// <paramref name="numeroConta"/> não é usado — a conta vem de <paramref name="origem"/>.
        /// </summary>
        internal override async Task<IReadOnlyList<Movimento>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, Correntista origem = null, CancellationToken cancellationToken = default)
        {
            if (tipoData != TipoDataConsultaEnum.Vencimento)
                throw new NotSupportedException("O DDA do Santander só filtra por data de vencimento (TipoDataConsultaEnum.Vencimento).");

            var titleSituation = MapSituacaoDda(situacao);
            var workspace = ResolverWorkspace(origem);

            const int limite = 200;
            var movimentos = new List<Movimento>();

            for (var pagina = 1; ; pagina++)
            {
                var url = $"{_baseUrl}workspaces/{workspace.WorkspaceId}/authorized_direct_debits?_limit={limite}&_PageNumber={pagina}" +
                          $"&initialDueDate={dataInicial:yyyy-MM-dd}&finalDueDate={dataFinal:yyyy-MM-dd}&titleSituation={titleSituation}";

                var dto = await _gateway.SendAsync<DdaListResponse>(HttpMethod.Get, url, body: null, cancellationToken).ConfigureAwait(false);
                if (dto?.Content == null || dto.Content.Count == 0)
                    break;

                movimentos.AddRange(dto.Content.Select(boleto => MapBoletoDda(boleto, situacao, titleSituation)));

                if (dto.Content.Count < limite)
                    break;
            }

            return movimentos;
        }

        #endregion

        #region ::. Workspace .::

        /// <summary>
        /// A workspace informada em <c>BancoApi.Conectar</c> (obtida por <c>ObterOuCriarWorkspaceAsync</c>)
        /// e a conta de débito da conta pagadora — sem consultar o banco.
        /// </summary>
        private (string WorkspaceId, ContaDebito Conta) ResolverWorkspace(Correntista origem) =>
            SantanderHttpGateway.UsarWorkspace(_idWorkspace, origem, "Boletos");

        #endregion

        #region ::. Mapeamento de resposta .::

        private static MovimentoItemPagamentoTituloCodigoBarra ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoTituloCodigoBarra;
            if (item == null)
                throw new InvalidOperationException("As operações de boleto esperam um MovimentoItemPagamentoTituloCodigoBarra em Movimento.MovimentoItem.");

            return item;
        }

        /// <summary>EmAberto traz todos os títulos em aberto (ALL), a vencer e vencidos.</summary>
        private static string MapSituacaoDda(SituacaoBoletoEnum situacao)
        {
            switch (situacao)
            {
                case SituacaoBoletoEnum.EmAberto: return "ALL";
                case SituacaoBoletoEnum.Agendado: return "SCHEDULED";
                case SituacaoBoletoEnum.Liquidado: return "PAID";
                case SituacaoBoletoEnum.Baixado: return "ISSUED";
                default: throw new ArgumentOutOfRangeException(nameof(situacao), situacao, "Situação de boleto DDA não suportada.");
            }
        }

        private static Movimento MapBoletoDda(DdaBoletoResponse dto, SituacaoBoletoEnum situacao, string titleSituation)
        {
            // A API não devolve a situação do título: ela é a do filtro usado na consulta.
            var item = new MovimentoItemDDA
            {
                CodigoTipoSituacaoBoleto = (int)situacao,
                DescricaoSituacaoBoleto = titleSituation,
                NumeroCodigoBarras = dto.Code.JustNumbers(),
                ValorBoleto = dto.NominalValue.GetValueOrDefault(),
                DataVencimentoBoleto = dto.DueDate.GetValueOrDefault(),
                DataLimitePagamento = dto.PaymentLimitDate,
                NumeroCpfCnpjPagador = dto.PayerDocumentNumber,
                NumeroNossoNumero = dto.BankIdentificationNumber,
                NumeroDocumento = dto.BeneficiaryIdentificationNumber,
                TipoPessoaBeneficiario = dto.Beneficiary?.DocumentType ?? dto.BeneficiaryType,
                NumeroCpfCnpjBeneficiario = dto.Beneficiary?.DocumentNumber ?? dto.BeneficiaryDocument,
                NomeRazaoSocialBeneficiario = dto.Beneficiary?.Name ?? dto.BeneficiaryName,
            };

            if (int.TryParse(dto.KindCode, out var especie))
                item.CodigoEspecieDocumento = especie;

            if (long.TryParse(dto.AuthorizedDirectDebitId, out var idCip))
                item.NumeroIdentificadorBoletoCip = idCip;

            return new Movimento { MovimentoItem = item };
        }

        private static Movimento AplicarResposta(Movimento movimento, MovimentoItemPagamentoTituloCodigoBarra item, PagamentoCodigoResponse dto, bool iniciacao)
        {
            if (dto == null)
                return movimento;

            if (!string.IsNullOrWhiteSpace(dto.Id))
            {
                item.IdentificadorConsulta = dto.Id;

                // Na iniciação o boleto ainda não foi pago, então o id fica só como identificador da consulta.
                if (!iniciacao)
                    movimento.NumeroDocumentoNoBanco = dto.Id;
            }

            SantanderHttpGateway.AplicarStatusCodigo(movimento, dto);

            var codigo = dto.Code.JustNumbers();
            if (codigo?.Length == 44)
                item.CodigoBarras = codigo;
            else if (codigo?.Length == 47)
                item.NumeroLinhaDigitavel = codigo;

            if (dto.DueDate.HasValue)
                item.DataVencimento = dto.DueDate;

            if (dto.NominalValue.HasValue)
                item.ValorCodigoBarra = dto.NominalValue.Value;

            if (dto.DeductedValue.HasValue)
                item.Desconto = dto.DeductedValue.Value;

            if (dto.AddedValue.HasValue)
                item.Acrescimo = dto.AddedValue.Value;

            if (!string.IsNullOrWhiteSpace(dto.Transaction?.Code))
                item.NumeroAutenticacaoPagamento = dto.Transaction.Code;

            if (dto.Payer != null)
            {
                item.PagadorConfirmadoNome = dto.Payer.Name;
                item.PagadorConfirmadoDocumento = dto.Payer.DocumentNumber;
                item.PagadorConfirmadoTipoPessoa = dto.Payer.DocumentType;
            }

            AplicarBeneficiario(movimento, dto.Beneficiary);

            return movimento;
        }

        /// <summary>
        /// Preenche o <see cref="Favorecido"/> com a identidade do beneficiário confirmada pelo
        /// banco, sem apagar o que o ERP já tinha quando o banco não devolve o campo.
        /// </summary>
        private static void AplicarBeneficiario(Movimento movimento, PixBeneficiario beneficiario)
        {
            if (beneficiario == null || (string.IsNullOrWhiteSpace(beneficiario.Name) && string.IsNullOrWhiteSpace(beneficiario.DocumentNumber)))
                return;

            if (movimento.Favorecido == null)
                movimento.Favorecido = new Favorecido();

            if (!string.IsNullOrWhiteSpace(beneficiario.Name))
                movimento.Favorecido.Nome = beneficiario.Name;

            if (!string.IsNullOrWhiteSpace(beneficiario.DocumentNumber))
            {
                movimento.Favorecido.CPF_CNPJ = beneficiario.DocumentNumber;
                movimento.Favorecido.TipoPessoa = string.Equals(beneficiario.DocumentType, "CNPJ", StringComparison.OrdinalIgnoreCase)
                    ? TipoInscricaoCPFCNPJEnum.CNPJ
                    : TipoInscricaoCPFCNPJEnum.CPF;
            }
        }

        #endregion
    }
}

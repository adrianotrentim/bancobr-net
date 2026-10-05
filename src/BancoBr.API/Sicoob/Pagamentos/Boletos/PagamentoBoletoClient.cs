using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.Models;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Errors;
using BancoBr.API.Sicoob.Pagamentos.Boletos.Models;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;

namespace BancoBr.API.Sicoob.Pagamentos.Boletos
{
    /// <summary>
    /// Cliente para a API "Pagamentos de Boletos" (Cobrança Bancária) do Sicoob, v3.
    ///
    /// Esta classe é o limite de mapeamento entre o contrato público
    /// (<see cref="Movimento"/>/<see cref="MovimentoItemPagamentoTituloCodigoBarra"/>,
    /// compartilhado com o CNAB) e os DTOs de wire do Sicoob (<c>Boletos.Models.*</c>, detalhe
    /// de implementação) — mesmo papel que BancoBr.CNAB.Base.Banco tem para os Segmentos, e
    /// igualmente por composição, nunca por herança.
    /// </summary>
    internal class PagamentoBoletoClient : PagamentoBoletoApiBase
    {
        /// <summary>
        /// Base URL, scopes e rate limit são intrínsecos a esta API específica do Sicoob
        /// (Pagamentos de Boletos v3) — não fazem sentido como configuração vinda do
        /// consumidor (ERP), que não deveria precisar conhecer detalhes da API do banco.
        /// </summary>
        public static readonly Uri BaseUrl = new Uri("https://api.sicoob.com.br/pagamentos/v3");

        /// <summary>
        /// Endpoint OAuth2 (client_credentials) do Sicoob, igual para qualquer API/produto e
        /// qualquer cooperado. Só precisa ser sobrescrito em cenários fora do padrão
        /// (ex.: um ambiente de testes com realm próprio) via o parâmetro tokenEndpoint.
        /// </summary>
        private static readonly Uri DefaultTokenEndpoint = new Uri("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token");

        private static readonly string[] Scopes = { "pagamentos_consulta", "pagamentos_inclusao", "pagamentos_alteracao" };

        private const int RequestsPerSecond = 2;

        /// <summary>0 - Conta Corrente (única modalidade habilitada para pagamento via API).</summary>
        private const int TipoContaCorrente = 0;

        private readonly IAccessTokenProvider _tokenProvider;
        private readonly string _clientId;
        private readonly string _baseUrl;
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings();

        internal PagamentoBoletoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint ?? DefaultTokenEndpoint))
        {
        }

        /// <summary>
        /// Usa o pipeline HTTP padrão (certificado mTLS + rate limiting) montado a partir de
        /// <paramref name="certificateSource"/>, mas com um provedor de token à escolha do
        /// chamador — por exemplo, <see cref="StaticAccessTokenProvider"/> quando o ambiente de
        /// sandbox fornece um Access Token (Bearer) pronto em vez de expor um endpoint OAuth2.
        /// </summary>
        internal PagamentoBoletoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(BuildHttpClient(certificateSource), tokenProvider, clientId, BaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake,
        /// sem certificado real nem chamadas HTTP de fato.
        /// </summary>
        public PagamentoBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        private static HttpClient BuildHttpClient(CertificateSource certificateSource)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            var rateLimiter = new RateLimitingHandler(RequestsPerSecond)
            {
                InnerHandler = certHandler,
            };

            return new HttpClient(rateLimiter);
        }

        private static OAuthTokenProvider BuildTokenProvider(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint)
        {
            var certHandler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
            };
            certHandler.ClientCertificates.Add(certificateSource.GetCertificate());

            var tokenHttpClient = new HttpClient(certHandler);
            var tokenOptions = new OAuthTokenProviderOptions
            {
                TokenEndpoint = tokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scopes = Scopes,
            };

            return new OAuthTokenProvider(tokenHttpClient, tokenOptions);
        }

        #region ::. Operações .::

        internal override async Task<Movimento> ConsultarBoletoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var consulta = await ConsultarBoletoWireAsync(item.CodigoBarras, origem.NumeroConta, DataPagamentoOuNulo(movimento), cancellationToken)
                .ConfigureAwait(false);

            if (consulta == null)
                return AplicarBoletoNaoEncontrado(movimento);

            AplicarConsulta(movimento, item, consulta);

            if (consulta.BloquearPagamento)
                AplicarBloqueio(movimento, consulta.MensagemBloqueioPagamento);

            return movimento;
        }

        internal override async Task<Movimento> PagarBoletoComConsultaAsync(Movimento movimento, Correntista origem, Guid idLancamento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var consulta = await ConsultarBoletoWireAsync(item.CodigoBarras, origem.NumeroConta, DataPagamentoOuNulo(movimento), cancellationToken)
                .ConfigureAwait(false);

            if (consulta == null)
                return AplicarBoletoNaoEncontrado(movimento);

            AplicarConsulta(movimento, item, consulta);

            if (consulta.BloquearPagamento)
                return AplicarBloqueio(movimento, consulta.MensagemBloqueioPagamento);

            var idempotencyKey = IdempotencyKey.New(origem.NumeroAgencia, origem.NumeroConta, idLancamento);
            return await PagarBoletoAsync(movimento, origem, idempotencyKey, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<IReadOnlyList<Base.Models.PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<(Movimento Movimento, Guid IdLancamento)> itens, Correntista origem, CancellationToken cancellationToken = default)
        {
            if (itens == null) throw new ArgumentNullException(nameof(itens));

            var resultados = new List<Base.Models.PagamentoBoletoLoteResultadoItem>();

            foreach (var item in itens)
            {
                try
                {
                    var movimento = await PagarBoletoComConsultaAsync(item.Movimento, origem, item.IdLancamento, cancellationToken).ConfigureAwait(false);

                    resultados.Add(Base.Models.PagamentoBoletoLoteResultadoItem.ComSucesso(movimento, item.IdLancamento));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    resultados.Add(Base.Models.PagamentoBoletoLoteResultadoItem.ComFalha(item.Movimento, item.IdLancamento, ex));
                }
            }

            return resultados;
        }

        internal override async Task<Movimento> PagarBoletoAsync(Movimento movimento, Correntista origem, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            var url = $"{_baseUrl}boletos/pagamentos/{(item.CodigoBarras ?? string.Empty).JustNumbers()}";
            var json = JsonConvert.SerializeObject(MontarRequisicaoPagamento(movimento, item, origem), SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Post, url, json, idempotencyKey), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Agendado;
                    movimento.DetalheRejeicaoBancoBr = "Pagamento pendente de assinatura.";
                    return movimento;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Efetivado;
                    return movimento;
                }

                try
                {
                    await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
                }
                catch (BancoApiException ex) when (ex.Mensagens.Any(m => m.Codigo == SicoobErrorCodes.IdempotencyJaUtilizado))
                {
                    // O pagamento já foi efetivado numa tentativa anterior com a mesma
                    // idempotency key; o comprovante é recuperado em vez de propagar o erro.
                    var comprovanteExistente = await ConsultarComprovanteWireAsync($"{_baseUrl}boletos/pagamentos/{idempotencyKey}/idempotency/comprovantes", cancellationToken).ConfigureAwait(false);
                    return AplicarComprovante(movimento, item, comprovanteExistente);
                }

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<ComprovantePagamento>>(body, SerializerSettings);
                return AplicarComprovante(movimento, item, envelope?.Resultado);
            }
        }

        internal override async Task<Movimento> ConsultarComprovantePorIdAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para consultar o comprovante, o movimento precisa ter o IdPagamento em NumeroDocumentoNoBanco.");

            var url = $"{_baseUrl}boletos/pagamentos/{movimento.NumeroDocumentoNoBanco}/comprovantes?numeroConta={origem.NumeroConta}";
            var comprovante = await ConsultarComprovanteWireAsync(url, cancellationToken).ConfigureAwait(false);

            return AplicarComprovante(movimento, item, comprovante);
        }

        internal override async Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            if (origem == null) throw new ArgumentNullException(nameof(origem));

            if (string.IsNullOrWhiteSpace(movimento.NumeroDocumentoNoBanco))
                throw new InvalidOperationException("Para cancelar o agendamento, o movimento precisa ter o IdPagamento em NumeroDocumentoNoBanco.");

            var url = $"{_baseUrl}boletos/pagamentos/agendamentos/{movimento.NumeroDocumentoNoBanco}";
            var json = JsonConvert.SerializeObject(new CancelamentoRequest { NumeroConta = origem.NumeroConta }, SerializerSettings);

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Delete, url, json, idempotencyKey: null), cancellationToken).ConfigureAwait(false))
            {
                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);
            }

            movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado;
            movimento.DetalheRejeicaoBancoBr = "Agendamento cancelado.";

            return movimento;
        }

        internal override async Task<Movimento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos/pagamentos/{idempotencyKey}/idempotency/comprovantes";
            var comprovante = await ConsultarComprovanteWireAsync(url, cancellationToken).ConfigureAwait(false);

            if (comprovante == null)
                return null;

            var movimento = new Movimento
            {
                TipoLancamento = TipoLancamentoEnum.PagamentoTituloOutroBanco,
                MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra(),
            };

            return AplicarComprovante(movimento, (MovimentoItemPagamentoTituloCodigoBarra)movimento.MovimentoItem, comprovante);
        }

        internal override async Task<IReadOnlyList<Movimento>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, Base.Models.SituacaoBoletoEnum situacao, Base.Models.TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}boletos?numeroConta={numeroConta}&dataInicial={dataInicial:yyyy-MM-dd}&dataFinal={dataFinal:yyyy-MM-dd}&situacao={(int)situacao}&tipoData={(int)tipoData}";

            using (var response = await SendWithAuthAsync(() => BuildRequest(HttpMethod.Get, url, body: null, idempotencyKey: null), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return Array.Empty<Movimento>();
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<BoletoDDA[]>>(body, SerializerSettings);
                return envelope?.Resultado?.Select(MapBoletoDda).ToList();
            }
        }

        #endregion

        #region ::. Chamadas de wire (DTOs do Sicoob) .::

        private async Task<BoletoConsultaResponse> ConsultarBoletoWireAsync(string codigoBarras, long numeroConta, DateTime? dataPagamento, CancellationToken cancellationToken)
        {
            var url = $"{_baseUrl}boletos/{(codigoBarras ?? string.Empty).JustNumbers()}?numeroConta={numeroConta}";
            if (dataPagamento.HasValue)
            {
                url += $"&dataPagamento={dataPagamento.Value:yyyy-MM-dd}";
            }

            return await SendAsync<BoletoConsultaResponse>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<ComprovantePagamento> ConsultarComprovanteWireAsync(string url, CancellationToken cancellationToken) =>
            await SendAsync<ComprovantePagamento>(HttpMethod.Get, url, body: null, idempotencyKey: null, cancellationToken).ConfigureAwait(false);

        #endregion

        #region ::. Mapeamento Movimento <-> Sicoob .::

        private static MovimentoItemPagamentoTituloCodigoBarra ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));

            var item = movimento.MovimentoItem as MovimentoItemPagamentoTituloCodigoBarra;
            if (item == null)
                throw new InvalidOperationException("As operações de boleto esperam um MovimentoItemPagamentoTituloCodigoBarra em Movimento.MovimentoItem.");

            return item;
        }

        private static DateTime? DataPagamentoOuNulo(Movimento movimento) =>
            movimento.DataPagamento == default(DateTime) ? (DateTime?)null : movimento.DataPagamento;

        /// <summary>
        /// O boleto não foi localizado pelo banco (204 sem conteúdo). Antes desta migração isto
        /// era um booleano dedicado (PagamentoBoletoResultado.BoletoNaoEncontrado); agora é
        /// expresso via situação + detalhe, sem perda de informação.
        /// </summary>
        private static Movimento AplicarBoletoNaoEncontrado(Movimento movimento)
        {
            movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado;
            movimento.DetalheRejeicaoBancoBr = "Boleto não encontrado.";
            return movimento;
        }

        /// <summary>
        /// O banco bloqueou o pagamento deste boleto — antes um booleano dedicado
        /// (PagamentoBoletoResultado.PagamentoBloqueado), hoje situação + detalhe.
        /// </summary>
        private static Movimento AplicarBloqueio(Movimento movimento, string mensagemBloqueio)
        {
            movimento.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado;
            movimento.DetalheRejeicaoBancoBr = mensagemBloqueio;
            return movimento;
        }

        private static void AplicarConsulta(Movimento movimento, MovimentoItemPagamentoTituloCodigoBarra item, BoletoConsultaResponse consulta)
        {
            item.BancoCodigoBarra = consulta.NumeroInstituicaoEmissora;
            item.NomeInstituicaoEmissora = consulta.NomeInstituicaoEmissora;

            if (!string.IsNullOrWhiteSpace(consulta.CodigoBarras))
                item.CodigoBarras = consulta.CodigoBarras;

            item.NumeroLinhaDigitavel = consulta.NumeroLinhaDigitavel;
            item.DataVencimento = consulta.DataVencimentoBoleto;
            item.DataLimitePagamento = consulta.DataLimitePagamentoBoleto;
            item.ValorCodigoBarra = consulta.ValorBoleto;
            item.Desconto = consulta.ValorAbatimentoDesconto;
            item.Acrescimo = consulta.ValorMultaMora;
            item.PermiteAlterarValor = consulta.PermiteAlterarValor;
            item.ConsultaEmContingencia = consulta.ConsultaEmContingencia;
            item.CodigoEspecieDocumento = consulta.CodigoEspecieDocumento;
            item.CodigoSituacaoBoletoPagamento = consulta.CodigoSituacaoBoletoPagamento;
            item.NossoNumero = consulta.NossoNumero;
            item.NumeroDocumentoTitulo = consulta.NumeroDocumento;
            item.IdentificadorConsulta = consulta.IdentificadorConsulta;
            item.DescricaoInstrucaoValorMinMax = consulta.DescricaoInstrucaoValorMinMax;
            item.NomeFantasiaBeneficiario = consulta.NomeFantasiaBeneficiario;

            item.PagadorConfirmadoNome = consulta.NomeRazaoSocialPagador;
            item.PagadorConfirmadoDocumento = consulta.NumeroCpfCnpjPagador;
            item.PagadorConfirmadoTipoPessoa = consulta.TipoPessoaPagador;

            AplicarBeneficiario(movimento, consulta.NomeRazaoSocialBeneficiario, consulta.NumeroCpfCnpjBeneficiario, consulta.TipoPessoaBeneficiario);

            movimento.ValorPagamento = consulta.ValorPagamento;

            // Só assume a data devolvida pelo banco quando o chamador não fixou uma — o
            // comportamento anterior (dataPagamento ?? consulta.DataPagamento) dava precedência
            // à data pedida pelo ERP.
            if (movimento.DataPagamento == default(DateTime))
                movimento.DataPagamento = consulta.DataPagamento;
        }

        private static Movimento AplicarComprovante(Movimento movimento, MovimentoItemPagamentoTituloCodigoBarra item, ComprovantePagamento comprovante)
        {
            if (comprovante == null)
                return movimento;

            movimento.NumeroDocumentoNoBanco = comprovante.IdPagamento.ToString();
            movimento.SituacaoBancoBr = MapSituacaoPagamentoParaSituacao(comprovante.SituacaoPagamento);
            movimento.DetalheRejeicaoBancoBr = comprovante.DescricaoDetalheSituacao;
            movimento.HorarioBancoBr = comprovante.DataHoraCadastro;

            if (comprovante.ValorPagamento != 0)
                movimento.ValorPagamento = comprovante.ValorPagamento;

            if (comprovante.DataPagamento != default(DateTime))
                movimento.DataPagamento = comprovante.DataPagamento;

            item.BancoCodigoBarra = comprovante.NumeroInstituicaoEmissora;
            item.NomeInstituicaoEmissora = comprovante.NomeInstituicaoEmissora;
            item.NumeroLinhaDigitavel = comprovante.NumeroLinhaDigitavel;

            if (comprovante.DataVencimento != default(DateTime))
                item.DataVencimento = comprovante.DataVencimento;

            item.ValorCodigoBarra = comprovante.ValorBoleto;
            item.Desconto = comprovante.ValorAbatimentoDesconto;
            item.Acrescimo = comprovante.ValorMultaMora;
            item.AceitaValorDivergente = comprovante.AceitaValorDivergente;
            item.NossoNumero = comprovante.NossoNumero;
            item.NumeroDocumentoTitulo = comprovante.NumeroDocumento;
            item.DescricaoObservacao = comprovante.DescricaoObservacao;
            item.DescricaoOuvidoria = comprovante.DescricaoOuvidoria;
            item.DescricaoTituloComprovante = comprovante.DescricaoTituloComprovante;
            item.NumeroAutenticacaoPagamento = comprovante.NumeroAutenticacaoPagamento;
            item.NomeAgencia = comprovante.NomeAgencia;
            item.NomeProprietarioContaCorrente = comprovante.NomeProprietarioContaCorrente;

            item.PagadorConfirmadoNome = comprovante.NomeRazaoSocialPagador;
            item.PagadorConfirmadoDocumento = comprovante.NumeroCpfCnpjPagador;

            AplicarBeneficiario(movimento, comprovante.NomeRazaoSocialBeneficiario, comprovante.NumeroCpfCnpjBeneficiario, tipoPessoa: null);

            return movimento;
        }

        /// <summary>
        /// Preenche o <see cref="Favorecido"/> com a identidade do beneficiário confirmada pelo
        /// banco, sem apagar o que o ERP já tinha quando o banco não devolve o campo.
        /// </summary>
        private static void AplicarBeneficiario(Movimento movimento, string nome, string documento, string tipoPessoa)
        {
            if (string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(documento))
                return;

            if (movimento.Favorecido == null)
                movimento.Favorecido = new Favorecido();

            if (!string.IsNullOrWhiteSpace(nome))
                movimento.Favorecido.Nome = nome;

            if (!string.IsNullOrWhiteSpace(documento))
            {
                movimento.Favorecido.CPF_CNPJ = documento;
                movimento.Favorecido.TipoPessoa = MapTipoPessoa(tipoPessoa, documento);
            }
        }

        /// <summary>
        /// ATENÇÃO: o Sicoob devolve "tipoPessoaBeneficiario"/"tipoPessoaPagador" como texto e o
        /// conjunto exato de valores não está confirmado em documentação disponível neste
        /// repositório. Reconhecemos os vocabulários plausíveis ("1"/"2" no padrão Febraban e
        /// variantes de "FISICA"/"JURIDICA") e, para qualquer outro valor, caímos na inferência
        /// pelo tamanho do documento (11 dígitos = CPF, 14 = CNPJ) — determinística e segura.
        /// O valor textual original fica preservado em
        /// <see cref="MovimentoItemPagamentoTituloCodigoBarra.PagadorConfirmadoTipoPessoa"/>.
        /// </summary>
        private static TipoInscricaoCPFCNPJEnum MapTipoPessoa(string tipoPessoa, string documento)
        {
            var normalizado = tipoPessoa?.Trim().ToUpperInvariant();

            if (normalizado == "1" || (normalizado != null && normalizado.Contains("FISICA")) || (normalizado != null && normalizado.Contains("FÍSICA")))
                return TipoInscricaoCPFCNPJEnum.CPF;

            if (normalizado == "2" || (normalizado != null && normalizado.Contains("JURIDICA")) || (normalizado != null && normalizado.Contains("JURÍDICA")))
                return TipoInscricaoCPFCNPJEnum.CNPJ;

            var digitos = (documento ?? string.Empty).JustNumbers();
            return digitos.Length > 11 ? TipoInscricaoCPFCNPJEnum.CNPJ : TipoInscricaoCPFCNPJEnum.CPF;
        }

        private static BoletoPagamentoRequest MontarRequisicaoPagamento(Movimento movimento, MovimentoItemPagamentoTituloCodigoBarra item, Correntista origem)
        {
            return new BoletoPagamentoRequest
            {
                IdentificadorConsulta = item.IdentificadorConsulta,
                ValorBoleto = item.ValorCodigoBarra,
                ValorDescontoAbatimento = item.Desconto,
                ValorMultaMora = item.Acrescimo,
                DescricaoObservacao = item.DescricaoObservacao,
                AceitaValorDivergente = item.AceitaValorDivergente,
                NumeroCpfCnpjPortador = origem.CPF_CNPJ.RemoveMascaraCpfCnpj(),
                NomePortador = origem.Nome,
                Amount = movimento.ValorPagamento,
                Date = movimento.DataPagamento,
                DebtorAccount = new DebtorAccount
                {
                    Issuer = origem.NumeroAgencia,
                    Number = origem.NumeroConta,
                    AccountType = TipoContaCorrente,
                    PersonType = origem.TipoPessoa == TipoInscricaoCPFCNPJEnum.CNPJ ? 1 : 0,
                },
            };
        }

        /// <summary>
        /// ATENÇÃO: mapeamento best-effort do campo textual "situacaoPagamento" retornado pela API
        /// de Pagamento de Boletos do Sicoob para o enum agnóstico
        /// <see cref="BancoBrSituacaoEnum"/>. O único valor confirmado em
        /// testes/documentação disponível neste repositório é "Efetivado". Os demais valores abaixo
        /// foram inferidos a partir do vocabulário plausível de status de pagamento de boleto e NÃO
        /// estão confirmados — DEVEM SER VALIDADOS/AJUSTADOS contra respostas reais do
        /// sandbox/produção do Sicoob antes de confiar neste mapeamento. Qualquer valor não
        /// reconhecido cai em NaoIntegrado, para nunca reportar falsamente Efetivado/Cancelado.
        /// </summary>
        private static BancoBrSituacaoEnum MapSituacaoPagamentoParaSituacao(string situacaoPagamento)
        {
            switch (situacaoPagamento?.Trim().ToUpperInvariant())
            {
                case "EFETIVADO":
                case "PAGO":
                case "LIQUIDADO":
                    return BancoBrSituacaoEnum.Efetivado;

                case "AGENDADO":
                case "EM_PROCESSAMENTO":
                case "PROCESSANDO":
                case "PENDENTE":
                    return BancoBrSituacaoEnum.Agendado;

                case "CANCELADO":
                case "NAO_EFETIVADO":
                case "DEVOLVIDO":
                    return BancoBrSituacaoEnum.Cancelado;

                case "REJEITADO":
                    return BancoBrSituacaoEnum.Rejeitado;

                default:
                    return BancoBrSituacaoEnum.NaoIntegrado;
            }
        }

        private static Movimento MapBoletoDda(BoletoDDA dto) => new Movimento
        {
            MovimentoItem = new MovimentoItemDDA
            {
                DescricaoTipoPagador = dto.DescricaoTipoPagador,
                TipoPessoaBeneficiario = dto.TipoPessoaBeneficiario,
                NumeroCpfCnpjBeneficiario = dto.NumeroCpfCnpjBeneficiario,
                NomeRazaoSocialBeneficiario = dto.NomeRazaoSocialBeneficiario,
                TipoPessoaPagador = dto.TipoPessoaPagador,
                NumeroCpfCnpjPagador = dto.NumeroCpfCnpjPagador,
                NomeRazaoSocialPagador = dto.NomeRazaoSocialPagador,
                NomeFantasiaPagador = dto.NomeFantasiaPagador,
                DescricaoLogradouroPagador = dto.DescricaoLogradouroPagador,
                DescricaoCidadePagador = dto.DescricaoCidadePagador,
                SiglaUfPagador = dto.SiglaUfPagador,
                NumeroCepPagador = dto.NumeroCepPagador,
                TipoPessoaAvalista = dto.TipoPessoaAvalista,
                NumeroCpfCnpjAvalista = dto.NumeroCpfCnpjAvalista,
                NomeAvalista = dto.NomeAvalista,
                ValorBoleto = dto.ValorBoleto,
                DataVencimentoBoleto = dto.DataVencimentoBoleto,
                CodigoTipoSituacaoBoleto = dto.CodigoTipoSituacaoBoleto,
                DescricaoSituacaoBoleto = dto.DescricaoSituacaoBoleto,
                NumeroIdentificadorBoletoCip = dto.NumeroIdentificadorBoletoCip,
                NumeroCodigoBarras = dto.NumeroCodigoBarras,
                NumeroCpfCnpjPagadorEletronico = dto.NumeroCpfCnpjPagadorEletronico,
                Aceite = dto.Aceite,
                NumeroNossoNumero = dto.NumeroNossoNumero,
                NumeroDocumento = dto.NumeroDocumento,
                DataPagamento = dto.DataPagamento,
                ValorPagamento = dto.ValorPagamento,
                CodigoEspecieDocumento = dto.CodigoEspecieDocumento,
                DataEmissao = dto.DataEmissao,
                DataLimitePagamento = dto.DataLimitePagamento,
                CodigoTipoJuros = dto.CodigoTipoJuros,
                DataJuros = dto.DataJuros,
                ValorPercentualJuros = dto.ValorPercentualJuros,
                CodigoTipoMulta = dto.CodigoTipoMulta,
                DataMulta = dto.DataMulta,
                ValorPercentualMulta = dto.ValorPercentualMulta,
                ValorAbatimento = dto.ValorAbatimento,
                CodigoTipoDesconto1 = dto.CodigoTipoDesconto1,
                DataDesconto1 = dto.DataDesconto1,
                ValorPercentualDesconto1 = dto.ValorPercentualDesconto1,
                CodigoTipoDesconto2 = dto.CodigoTipoDesconto2,
                DataDesconto2 = dto.DataDesconto2,
                ValorPercentualDesconto2 = dto.ValorPercentualDesconto2,
                CodigoTipoDesconto3 = dto.CodigoTipoDesconto3,
                DataDesconto3 = dto.DataDesconto3,
                ValorPercentualDesconto3 = dto.ValorPercentualDesconto3,
                NumeroDiasProtesto = dto.NumeroDiasProtesto,
                QuantidadePagamentoParcial = dto.QuantidadePagamentoParcial,
                CodigoAutorizacaoValorDivergente = dto.CodigoAutorizacaoValorDivergente,
                CodigoIndicadorValorMaximo = dto.CodigoIndicadorValorMaximo,
                ValorPercentualMaximo = dto.ValorPercentualMaximo,
                CodigoIndicadorValorMinimo = dto.CodigoIndicadorValorMinimo,
                ValorPercentualMinimo = dto.ValorPercentualMinimo,
            },
        };

        #endregion

        #region ::. Plumbing HTTP .::

        private async Task<T> SendAsync<T>(HttpMethod method, string url, string body, string idempotencyKey, CancellationToken cancellationToken)
        {
            using (var response = await SendWithAuthAsync(() => BuildRequest(method, url, body, idempotencyKey), cancellationToken).ConfigureAwait(false))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return default;
                }

                await EnsureSuccessOrThrowAsync(response).ConfigureAwait(false);

                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var envelope = JsonConvert.DeserializeObject<ResultadoEnvelope<T>>(responseBody, SerializerSettings);
                return envelope.Resultado;
            }
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body, string idempotencyKey)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("client_id", _clientId);

            if (idempotencyKey != null)
            {
                request.Headers.Add("x-idempotency-key", idempotencyKey);
            }

            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            return request;
        }

        private async Task<HttpResponseMessage> SendWithAuthAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            var response = await SendOnceAsync(requestFactory, token, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                _tokenProvider.InvalidateToken();
                token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                response = await SendOnceAsync(requestFactory, token, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }

        private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> requestFactory, string token, CancellationToken cancellationToken)
        {
            using (var request = requestFactory())
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            SicoobErrorResponse errorResponse;
            try
            {
                errorResponse = JsonConvert.DeserializeObject<SicoobErrorResponse>(body, SerializerSettings);
            }
            catch (JsonException)
            {
                errorResponse = null;
            }

            throw new BancoApiException((int)response.StatusCode, errorResponse?.Mensagens ?? new System.Collections.Generic.List<MensagemErro>());
        }

        #endregion
    }
}

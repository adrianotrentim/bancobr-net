using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Core.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Santander.ContaCorrente.Models;
using BancoBr.API.Santander.Http;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.API.Santander.ContaCorrente
{
    /// <summary>
    /// Cliente para a API "Consulta de Extrato" (bank_account_information v1) do Santander. A
    /// conta é identificada por <c>origem</c> (agência 4 dígitos + "." + conta com DV em 12
    /// dígitos). A API não devolve saldo: a lista traz só os lançamentos
    /// (<see cref="MovimentoItemExtratoTransacao"/>), já paginados de 50 em 50.
    /// </summary>
    internal class ExtratoClient : ContaCorrenteApiBase
    {
        /// <summary>CNPJ do Santander — extrato de outras instituições exige consentimento (Open Finance).</summary>
        private const string BancoId = "90400888000142";

        private const int Limite = 50;

        private readonly SantanderHttpGateway _gateway;
        private readonly string _baseUrl;

        internal ExtratoClient(string clientId, string clientSecret, CertificateSource certificateSource, Uri tokenEndpoint = null)
            : this(clientId, certificateSource, SantanderHttpGateway.BuildTokenProvider(clientId, clientSecret, certificateSource, tokenEndpoint))
        {
        }

        internal ExtratoClient(string clientId, CertificateSource certificateSource, IAccessTokenProvider tokenProvider)
            : this(SantanderHttpGateway.BuildHttpClient(certificateSource), tokenProvider, clientId, SantanderHttpGateway.ContaCorrenteBaseUrl)
        {
        }

        /// <summary>
        /// Construtor para testes: permite injetar um HttpClient/IAccessTokenProvider fake.
        /// </summary>
        public ExtratoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, string clientId, Uri baseUrl)
            : base(httpClient)
        {
            if (baseUrl == null) throw new ArgumentNullException(nameof(baseUrl));

            _gateway = new SantanderHttpGateway(httpClient, tokenProvider, clientId);
            var baseUrlText = baseUrl.ToString();
            _baseUrl = baseUrlText.EndsWith("/") ? baseUrlText : baseUrlText + "/";
        }

        /// <summary>
        /// Consulta o período <paramref name="mes"/>/<paramref name="ano"/> (recortado por
        /// <paramref name="diaInicial"/>/<paramref name="diaFinal"/>) da conta de
        /// <paramref name="origem"/>. <paramref name="numeroContaCorrente"/> e
        /// <paramref name="agruparCnab"/> não são usados.
        /// </summary>
        internal override async Task<IReadOnlyList<Movimento>> ConsultarExtratoAsync(long numeroContaCorrente, int mes, int ano, int? diaInicial, int? diaFinal, bool agruparCnab, CancellationToken cancellationToken = default, Correntista origem = null)
        {
            if (origem == null)
                throw new ArgumentNullException(nameof(origem), "O Santander exige a conta (origem: agência/conta/DV) para consultar o extrato.");

            var dataInicial = new DateTime(ano, mes, diaInicial ?? 1);
            var dataFinal = new DateTime(ano, mes, diaFinal ?? DateTime.DaysInMonth(ano, mes));

            var conta = $"{origem.NumeroConta}{origem.DVConta}".ToUpperInvariant().PadLeft(12, '0');
            var url = $"{_baseUrl}banks/{BancoId}/statements/{origem.NumeroAgencia:D4}.{conta}?initialDate={dataInicial:yyyy-MM-dd}&finalDate={dataFinal:yyyy-MM-dd}";

            var movimentos = new List<Movimento>();

            while (true)
            {
                var dto = await _gateway.SendAsync<ExtratoResponse>(HttpMethod.Get, $"{url}&_limit={Limite}&_offset={movimentos.Count}", body: null, cancellationToken).ConfigureAwait(false);
                if (dto?.Content == null || dto.Content.Count == 0)
                    break;

                foreach (var transacao in dto.Content)
                    movimentos.Add(MapTransacao(transacao));

                if (dto.Content.Count < Limite)
                    break;
            }

            return movimentos;
        }

        private static Movimento MapTransacao(ExtratoTransacaoResponse dto) => new Movimento
        {
            MovimentoItem = new MovimentoItemExtratoTransacao
            {
                Tipo = MapTipo(dto.CreditDebitType),
                Valor = Math.Abs(dto.Amount),
                Data = dto.TransactionDate,
                Descricao = dto.TransactionName,
            },
        };

        /// <summary>
        /// Sem fallback, como no Sicoob: errar o sinal de um lançamento financeiro é pior do que
        /// falhar a importação.
        /// </summary>
        private static BancoBrTipoCreditoDebitoEnum MapTipo(string tipo)
        {
            switch (tipo?.Trim().ToUpperInvariant())
            {
                case "CREDITO":
                case "CREDIT":
                case "C":
                    return BancoBrTipoCreditoDebitoEnum.Credito;

                case "DEBITO":
                case "DEBIT":
                case "D":
                    return BancoBrTipoCreditoDebitoEnum.Debito;

                default:
                    throw new Exception($"Tipo de lançamento não reconhecido no extrato Santander: \"{tipo}\".");
            }
        }
    }
}

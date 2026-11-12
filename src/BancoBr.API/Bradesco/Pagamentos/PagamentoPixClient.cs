using System;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Bradesco.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.API.Sicoob.Pagamentos.Ted;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.Pagamentos
{
    /// <summary>
    /// API Pix Transferências do Bradesco (<c>v1/spi</c>): a transferência é feita em uma única chamada,
    /// por chave Pix (<see cref="MovimentoItemTransferenciaPIX.ChavePIX"/>) ou por agência e conta do
    /// favorecido, a partir da conta pagadora de <c>ConfiguracaoAPI.Correntista</c>. Não há iniciação
    /// separada da confirmação, nem Pix Copia e Cola.
    ///
    /// O <c>idtransacao</c> é do pagador, único e imutável: vem de <see cref="Movimento.NumeroDocumento"/>
    /// (1 a 35 caracteres alfanuméricos), por isso o mesmo movimento reenviado não duplica o Pix — o banco
    /// recusa o identificador repetido. O EndToEndId devolvido fica em <see cref="Movimento.NumeroDocumentoNoBanco"/>.
    /// Resultado <c>EM_PROCESSAMENTO</c> aparece como <see cref="BancoBrSituacaoEnum.Agendado"/>: consulte em até 1 minuto.
    /// </summary>
    internal class PagamentoPixClient : PagamentoPixApiBase
    {
        private static readonly Regex IdTransacaoValido = new Regex("^[A-Za-z0-9]{1,35}$", RegexOptions.Compiled);
        private static readonly Regex E2eValido = new Regex("^[A-Za-z0-9]{32}$", RegexOptions.Compiled);

        private readonly BradescoHttpGateway _gateway;
        private readonly string _baseUrl;

        internal PagamentoPixClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, Correntista origem, Uri baseUrl = null)
            : base(httpClient, origem)
        {
            _gateway = new BradescoHttpGateway(httpClient, tokenProvider);
            _baseUrl = BradescoHttpGateway.NormalizarBase(baseUrl ?? BradescoHttpGateway.PixBaseUrl);
        }

        internal override async Task<Movimento> PagarComIniciacaoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            if (movimento.ValorPagamento <= 0)
                throw new ArgumentException("Informe o valor do Pix (Movimento.ValorPagamento).", nameof(movimento));

            var corpo = new JObject
            {
                ["pagador"] = MontarPagador(),
                ["recebedor"] = MontarRecebedor(movimento, item),
                ["idtransacao"] = IdTransacao(movimento),
                ["valor"] = movimento.ValorPagamento.ToString("0.00", CultureInfo.InvariantCulture),
            };

            var resposta = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "v1/spi/solicitar-transferencia", corpo, cancellationToken).ConfigureAwait(false);
            return AplicarResposta(movimento, item, resposta);
        }

        /// <summary>Com o EndToEndId a consulta usa idtransacao + e2e; sem ele (ou fora do formato), só o idtransacao.</summary>
        internal override async Task<Movimento> ConsultarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var url = _baseUrl + "v1/spi/consulta/transferencia/" + Uri.EscapeDataString(IdTransacao(movimento));
            var e2e = movimento.NumeroDocumentoNoBanco?.Trim();
            if (e2e != null && E2eValido.IsMatch(e2e))
                url += "/" + e2e;

            var resposta = await _gateway.SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
            return AplicarResposta(movimento, item, resposta);
        }

        internal override Task<Movimento> IniciarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API Pix do Bradesco não resolve a chave antes do pagamento: use EnviarAgendamentoAsync.");

        internal override Task<Movimento> ConfirmarPagamentoAsync(Movimento movimento, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API Pix do Bradesco não tem confirmação separada: o Pix já foi enviado; use ConsultarAgendamentoAsync para ver a situação.");

        internal override Task<Movimento> PagarViaQrCodeAsync(Movimento movimento, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("O Pix Copia e Cola não está disponível na API de Pix Transferências do Bradesco.");

        #region ::. Mapeamento Movimento <-> Bradesco .::

        private static MovimentoItemTransferenciaPIX ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            return movimento.MovimentoItem as MovimentoItemTransferenciaPIX
                ?? throw new ArgumentException("Informe um MovimentoItemTransferenciaPIX em Movimento.MovimentoItem.", nameof(movimento));
        }

        private static string IdTransacao(Movimento movimento)
        {
            var id = movimento.NumeroDocumento?.Trim();
            if (id == null || !IdTransacaoValido.IsMatch(id))
                throw new ArgumentException("O Pix do Bradesco exige em Movimento.NumeroDocumento um identificador único de 1 a 35 caracteres alfanuméricos (idtransacao). Preserve-o nos reenvios.", nameof(movimento));
            return id;
        }

        /// <summary>A conta pagadora é a de <c>ConfiguracaoAPI.Correntista</c>, informada por agência e conta (com dígito).</summary>
        private JObject MontarPagador()
        {
            if (Origem == null || Origem.NumeroAgencia <= 0 || Origem.NumeroConta <= 0)
                throw new InvalidOperationException("O Pix do Bradesco exige agência e conta da conta pagadora (ConfiguracaoAPI.Correntista).");

            return new JObject
            {
                ["tipoChave"] = "AGENCIACONTA",
                ["agencia"] = Origem.NumeroAgencia.ToString(CultureInfo.InvariantCulture),
                ["conta"] = Origem.NumeroConta.ToString(CultureInfo.InvariantCulture) + (Origem.DVConta ?? string.Empty).Trim(),
            };
        }

        /// <summary>
        /// Com chave Pix, o CPF/CNPJ e o nome do favorecido, quando informados, seguem junto: o banco passa a
        /// validar se a chave pertence àquele documento e recusa o Pix se não pertencer.
        /// </summary>
        private static JObject MontarRecebedor(Movimento movimento, MovimentoItemTransferenciaPIX item)
        {
            var favorecido = movimento.Favorecido;
            var documento = BradescoCampos.Digitos(favorecido?.CPF_CNPJ);
            var nome = BradescoCampos.Truncar(favorecido?.Nome, 60);

            if (string.IsNullOrWhiteSpace(item.ChavePIX))
                return MontarRecebedorPorConta(item, documento, nome);

            var recebedor = new JObject
            {
                ["tipoChave"] = MapTipoChave(item.TipoChavePIX),
                ["chavePix"] = NormalizarChave(item),
            };
            if (documento.Length > 0) recebedor["cpfCnpj"] = documento;
            if (nome.Length > 0) recebedor["nomeFavorecido"] = nome;
            return recebedor;
        }

        private static JObject MontarRecebedorPorConta(MovimentoItemTransferenciaPIX item, string documento, string nome)
        {
            if (documento.Length == 0 || nome.Length == 0)
                throw new InvalidOperationException("O Pix por agência e conta exige o nome e o CPF/CNPJ do favorecido (Movimento.Favorecido).");
            if (item.NumeroAgencia <= 0 || item.NumeroConta <= 0)
                throw new InvalidOperationException("O Pix por agência e conta exige agência e conta do favorecido no movimento.");
            if (item.Banco <= 0 && string.IsNullOrWhiteSpace(item.Ispb))
                throw new InvalidOperationException("O Pix por agência e conta exige o banco (Banco) ou o ISPB (Ispb) do favorecido.");

            var recebedor = new JObject
            {
                ["cpfCnpj"] = documento,
                ["tipoChave"] = "AGENCIACONTA",
                ["agencia"] = item.NumeroAgencia.ToString(CultureInfo.InvariantCulture),
                ["ispb"] = !string.IsNullOrWhiteSpace(item.Ispb) ? item.Ispb.Trim() : TabelaIspbPorCompe.ObterIspb(item.Banco),
                ["conta"] = item.NumeroConta.ToString(CultureInfo.InvariantCulture),
                ["digitoConta"] = (item.DVConta ?? string.Empty).Trim(),
                ["tipoConta"] = MapTipoConta(item.TipoConta),
                ["nomeFavorecido"] = nome,
            };
            if (item.Banco > 0) recebedor["banco"] = item.Banco.ToString("D3", CultureInfo.InvariantCulture);
            return recebedor;
        }

        private static string MapTipoChave(FormaIniciacaoEnum tipo)
        {
            switch (tipo)
            {
                case FormaIniciacaoEnum.PIX_Telefone:
                    return "TELEFONE";

                case FormaIniciacaoEnum.PIX_Email:
                    return "EMAIL";

                case FormaIniciacaoEnum.PIX_CPF_CNPJ:
                    return "CPFCNPJ";

                case FormaIniciacaoEnum.PIX_Aleatorio:
                    return "EVP";

                default:
                    throw new NotSupportedException("Tipo de chave Pix não suportado.");
            }
        }

        private static string MapTipoConta(TipoContaEnum tipo)
        {
            switch (tipo)
            {
                case TipoContaEnum.ContaPoupanca:
                    return "CONTA_POUPANCA";

                case TipoContaEnum.ContaPagamento:
                    return "CONTA_PAGAMENTO";

                default:
                    return "CONTA_CORRENTE";
            }
        }

        /// <summary>CPF/CNPJ só com dígitos; telefone no formato do DICT (+55DDDNÚMERO); e-mail e chave aleatória como informados.</summary>
        private static string NormalizarChave(MovimentoItemTransferenciaPIX item)
        {
            var chave = item.ChavePIX.Trim();
            switch (item.TipoChavePIX)
            {
                case FormaIniciacaoEnum.PIX_CPF_CNPJ:
                    return BradescoCampos.Digitos(chave);

                case FormaIniciacaoEnum.PIX_Telefone:
                    var digitos = BradescoCampos.Digitos(chave);
                    if (digitos.Length == 10 || digitos.Length == 11) return "+55" + digitos;
                    if ((digitos.Length == 12 || digitos.Length == 13) && digitos.StartsWith("55")) return "+" + digitos;
                    throw new ArgumentException("Informe o telefone Pix com DDD (ex.: +5511999999999).", nameof(item));

                default:
                    return chave;
            }
        }

        private static Movimento AplicarResposta(Movimento movimento, MovimentoItemTransferenciaPIX item, JObject resposta)
        {
            var e2e = BradescoCampos.Texto(resposta["e2e"]);
            if (e2e != null)
                movimento.NumeroDocumentoNoBanco = e2e;

            var mensagem = BradescoCampos.Texto(resposta["mensagem"]);
            switch (BradescoCampos.Texto(resposta["status"])?.ToUpperInvariant())
            {
                case "CONCLUIDO":
                    movimento.Situacao = BancoBrSituacaoEnum.Efetivado;
                    break;

                case "EM_PROCESSAMENTO":
                    movimento.Situacao = BancoBrSituacaoEnum.Agendado;
                    break;

                case "REJEITADO":
                    movimento.Situacao = BancoBrSituacaoEnum.Rejeitado;
                    break;

                default:
                    movimento.Situacao = BancoBrSituacaoEnum.NaoIntegrado;
                    break;
            }
            movimento.DetalheRejeicao = movimento.Situacao == BancoBrSituacaoEnum.Rejeitado ? mensagem ?? "Pix rejeitado." : null;

            if (DateTime.TryParse(BradescoCampos.Texto(resposta["dataEfetivacao"]) ?? BradescoCampos.Texto(resposta["dataCriacao"]),
                    CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var horario))
                movimento.Horario = horario;

            var recebedor = resposta["recebedor"] as JObject;
            if (recebedor != null)
            {
                item.TipoChaveConfirmado = BradescoCampos.Texto(recebedor["tipoChave"]) ?? item.TipoChaveConfirmado;
                item.TitularConfirmadoNome = BradescoCampos.Texto(recebedor["nomeFavorecido"]) ?? item.TitularConfirmadoNome;
                item.TitularConfirmadoDocumento = BradescoCampos.Texto(recebedor["cpfCnpj"]) ?? item.TitularConfirmadoDocumento;
            }

            return movimento;
        }

        #endregion
    }
}

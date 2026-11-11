using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Bradesco.Http;
using BancoBr.API.Core.Errors;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.Pagamentos
{
    /// <summary>
    /// API de TED do Bradesco (<c>transferencia/ted/v1</c>): envio direto, sem consulta prévia. Só
    /// efetiva no dia útil entre 06h30 e 17h; a API não oferece agendamento nem cancelamento e não
    /// deduplica reenvios — antes de repetir um envio sem resposta, confira o lançamento por outro canal.
    ///
    /// O banco devolve <c>chaveUnicaParaApi</c> (7 caracteres do número do documento + data), que fica em
    /// <see cref="Movimento.NumeroDocumentoNoBanco"/> e é o que <see cref="ConsultarTedAsync"/> usa.
    /// A TED só é considerada <see cref="BancoBrSituacaoEnum.Efetivado"/> pela consulta, quando o banco
    /// destinatário aceita; até lá fica <see cref="BancoBrSituacaoEnum.Agendado"/>, pois ele ainda pode devolver.
    /// </summary>
    internal class PagamentoTedClient : PagamentoTedApiBase
    {
        private const long CodigoRetornoSucesso = 23;

        private readonly BradescoHttpGateway _gateway;
        private readonly string _baseUrl;

        internal PagamentoTedClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, Correntista origem, Uri baseUrl = null)
            : base(httpClient, origem)
        {
            _gateway = new BradescoHttpGateway(httpClient, tokenProvider);
            _baseUrl = BradescoHttpGateway.NormalizarBase(baseUrl ?? BradescoHttpGateway.PagamentosBaseUrl);
        }

        internal override async Task<Movimento> PagarTedAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var corpo = MontarRequisicao(movimento, item);
            var resposta = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "transferencia/ted/v1/efetiva", corpo, cancellationToken).ConfigureAwait(false);
            return AplicarEfetivacao(movimento, item, resposta);
        }

        internal override async Task<Movimento> ConsultarTedAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            var chave = movimento.NumeroDocumentoNoBanco?.Trim();
            if (string.IsNullOrEmpty(chave) || chave.Length < 7)
                throw new InvalidOperationException("Para consultar uma TED, o movimento precisa ter a chave devolvida por PagarTedAsync em NumeroDocumentoNoBanco.");

            DateTime data;
            if (!TryDataDaChave(chave, out data))
            {
                if (movimento.DataPagamento == default(DateTime))
                    throw new InvalidOperationException("Não foi possível obter a data da TED: informe Movimento.DataPagamento ou a chave completa em NumeroDocumentoNoBanco.");
                data = movimento.DataPagamento;
            }

            var url = $"{_baseUrl}transferencia/ted/v1/consulta?numeroDocumento={Uri.EscapeDataString(chave.Substring(0, 7))}&dataOperacao={BradescoCampos.DataComPontos(data)}";
            var resposta = await _gateway.SendAsync(HttpMethod.Get, url, null, cancellationToken).ConfigureAwait(false);
            return AplicarConsulta(movimento, item, resposta);
        }

        internal override Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de TED do Bradesco não oferece agendamento nem cancelamento.");

        #region ::. Mapeamento Movimento <-> Bradesco .::

        private static MovimentoItemTransferenciaTED ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            return movimento.MovimentoItem as MovimentoItemTransferenciaTED
                ?? throw new ArgumentException("Informe um MovimentoItemTransferenciaTED em Movimento.MovimentoItem.", nameof(movimento));
        }

        private JObject MontarRequisicao(Movimento movimento, MovimentoItemTransferenciaTED item)
        {
            var favorecido = movimento.Favorecido;
            if (favorecido == null || string.IsNullOrWhiteSpace(favorecido.Nome) || string.IsNullOrWhiteSpace(favorecido.CPF_CNPJ))
                throw new InvalidOperationException("A TED exige o nome e o CPF/CNPJ do favorecido (Movimento.Favorecido).");
            if (Origem == null || string.IsNullOrWhiteSpace(Origem.CPF_CNPJ))
                throw new InvalidOperationException("A TED do Bradesco exige o CPF/CNPJ do titular da conta pagadora (ConfiguracaoAPI.Correntista).");
            if (item.Banco <= 0 || item.NumeroAgencia <= 0 || item.NumeroConta <= 0)
                throw new InvalidOperationException("A TED exige banco, agência e conta do favorecido no movimento.");
            if (item.CodigoFinalidadeTED == FinalidadeTEDEnum.NaoAplicavel)
                throw new InvalidOperationException("A TED do Bradesco exige a finalidade (CodigoFinalidadeTED) no movimento.");
            if (movimento.ValorPagamento <= 0)
                throw new InvalidOperationException("Informe o valor da TED (Movimento.ValorPagamento).");

            var destino = BradescoCampos.DividirDocumento(favorecido.CPF_CNPJ);
            var mesmaTitularidade = BradescoCampos.Digitos(favorecido.CPF_CNPJ) == BradescoCampos.Digitos(Origem.CPF_CNPJ);
            var data = movimento.DataPagamento == default(DateTime) ? DateTime.Today : movimento.DataPagamento;

            var corpo = new JObject
            {
                ["identificadorDoTipoDeTransferencia"] = 1,
                ["agenciaRemetente"] = Origem.NumeroAgencia,
                ["bancoDestinatario"] = item.Banco,
                ["agenciaDestinatario"] = item.NumeroAgencia,
                ["contaRemetenteComDigito"] = BradescoCampos.ContaComDigito(Origem.NumeroConta, Origem.DVConta),
                ["tipoContaRemetente"] = "CC",
                ["tipoDePessoaRemetente"] = BradescoCampos.TipoPessoa(Origem.CPF_CNPJ),
                ["contaDestinatario"] = BradescoCampos.ContaComDigito(item.NumeroConta, item.DVConta),
                ["tipoDeContaDestinatario"] = MapTipoConta(item.TipoConta),
                ["tipodePessoaDestinatario"] = BradescoCampos.TipoPessoa(favorecido.CPF_CNPJ),
                ["numeroInscricao"] = destino.Raiz.ToString(CultureInfo.InvariantCulture).PadLeft(9, '0'),
                ["numeroFilial"] = destino.Filial.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0'),
                ["numeroControle"] = destino.Controle.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0'),
                ["nomeClienteDestinatario"] = BradescoCampos.Truncar(favorecido.Nome, 80),
                ["valorDaTransferencia"] = movimento.ValorPagamento,
                ["finalidadeDaTransferencia"] = (int)item.CodigoFinalidadeTED,
                ["codigoIdentificadorDaTransferencia"] = BradescoCampos.Truncar(movimento.NumeroDocumento, 25),
                ["dataMovimento"] = BradescoCampos.DataComPontos(data),
                ["tipoDeDoc"] = mesmaTitularidade ? "D" : "E",
                ["tipoDeDocumentoDeBarras"] = "",
                ["numeroCodigoDeBarras"] = "",
                ["canalPagamento"] = 0,
                ["valorMulta"] = 0,
                ["valorJuro"] = 0,
                ["valorDescontoOuAbatimento"] = 0,
                ["valorOutrosAcrescimos"] = 0,
                ["indicadorDda"] = "N",
            };

            if (!string.IsNullOrWhiteSpace(Origem.Nome))
                corpo["nomeClienteRemetente"] = BradescoCampos.Truncar(Origem.Nome, 80);

            return corpo;
        }

        /// <summary>A API só aceita conta-corrente e poupança.</summary>
        private static string MapTipoConta(TipoContaEnum tipo)
        {
            switch (tipo)
            {
                case TipoContaEnum.ContaCorrente:
                    return "CC";

                case TipoContaEnum.ContaPoupanca:
                    return "PP";

                default:
                    throw new NotSupportedException("A TED do Bradesco aceita apenas conta-corrente e conta-poupança no favorecido.");
            }
        }

        /// <summary>Sucesso é <c>codigoDeRetorno</c> 23 sem <c>codigoDeErro</c>; qualquer outro resultado é rejeição.</summary>
        private static Movimento AplicarEfetivacao(Movimento movimento, MovimentoItemTransferenciaTED item, JObject resposta)
        {
            var mensagem = BradescoCampos.Texto(resposta["mensagem"]);
            var codigoErro = BradescoCampos.Texto(resposta["codigoDeErro"]);
            var sucesso = BradescoCampos.Inteiro(resposta["codigoDeRetorno"]) == CodigoRetornoSucesso && !TemErro(codigoErro);

            item.Situacao = mensagem;
            item.Agendado = false;

            if (!sucesso)
            {
                movimento.Situacao = BancoBrSituacaoEnum.Rejeitado;
                movimento.DetalheRejeicao = mensagem ?? codigoErro;
                item.MensagemErro = movimento.DetalheRejeicao;
                return movimento;
            }

            var chave = BradescoCampos.Texto(resposta["chaveUnicaParaApi"]);
            if (chave != null)
            {
                movimento.NumeroDocumentoNoBanco = chave;
                item.NumeroControleIF = chave.Length >= 7 ? chave.Substring(0, 7) : chave;
            }

            movimento.Situacao = BancoBrSituacaoEnum.Agendado;
            movimento.DetalheRejeicao = null;
            item.MensagemErro = null;
            AplicarBancoDestino(item, resposta["bancoDestinatario"]);
            return movimento;
        }

        private static Movimento AplicarConsulta(Movimento movimento, MovimentoItemTransferenciaTED item, JObject resposta)
        {
            var status = BradescoCampos.Texto(resposta["statusMensagem"])?.ToUpperInvariant();
            if (status == null)
            {
                var codigoErro = BradescoCampos.Texto(resposta["codigoErro"]);
                var mensagem = BradescoCampos.Texto(resposta["mensagem"]);
                if (mensagem != null || TemErro(codigoErro))
                    throw new BancoApiException(null, new[] { new MensagemErro { Codigo = codigoErro, Mensagem = mensagem } });
            }

            switch (status)
            {
                case "ACEITA":
                case "OK":
                    movimento.Situacao = BancoBrSituacaoEnum.Efetivado;
                    movimento.DetalheRejeicao = null;
                    break;

                case "DEVOLVIDA":
                    movimento.Situacao = BancoBrSituacaoEnum.Rejeitado;
                    movimento.DetalheRejeicao = BradescoCampos.Texto(resposta["descricaoDaDevolucao"]) ?? "TED devolvida.";
                    break;

                default:
                    movimento.Situacao = BancoBrSituacaoEnum.NaoIntegrado;
                    break;
            }

            item.Situacao = status;
            item.MensagemErro = movimento.DetalheRejeicao;
            item.Agendado = false;
            AplicarBancoDestino(item, resposta["bancoDestinatario"]);

            var valor = BradescoCampos.Decimal(resposta["valorDaTransferencia"]);
            if (movimento.ValorPagamento == 0 && valor.GetValueOrDefault() != 0)
                movimento.ValorPagamento = valor.Value;

            return movimento;
        }

        private static void AplicarBancoDestino(MovimentoItemTransferenciaTED item, JToken banco)
        {
            var codigo = BradescoCampos.Inteiro(banco);
            if (codigo.HasValue)
                item.NumeroBancoFavorecido = (int)codigo.Value;
        }

        private static bool TemErro(string codigo) => !string.IsNullOrWhiteSpace(codigo) && codigo.Trim('0').Length > 0;

        /// <summary>
        /// A chave da efetivação é "7 caracteres + AAAA-MM-DD-hh.mm.ss.ffffff" e a da consulta, "7 caracteres +
        /// DD.MM.AAAA"; nos dois casos a data da operação vem logo após o número do documento.
        /// </summary>
        private static bool TryDataDaChave(string chave, out DateTime data)
        {
            data = default(DateTime);
            return chave.Length >= 17 && DateTime.TryParseExact(chave.Substring(7, 10), new[] { "dd.MM.yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out data);
        }

        #endregion
    }
}

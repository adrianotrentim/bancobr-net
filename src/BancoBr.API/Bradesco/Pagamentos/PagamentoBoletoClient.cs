using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base;
using BancoBr.API.Base.Models;
using BancoBr.API.Bradesco.Http;
using BancoBr.API.Core.OAuth;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.Pagamentos
{
    /// <summary>
    /// API de Pagamento de Boletos de Cobrança do Bradesco (<c>boleto/pagamento-cobranca/v1</c>), que atende
    /// boletos do Bradesco e de outros bancos pelo código de barras ou pela linha digitável.
    ///
    /// <para><b>Consulta</b> (<see cref="ConsultarBoletoAsync"/>): valida o título (base Núclea/CIP) e faz a
    /// pré-efetivação, que devolve o valor calculado, o vencimento e os dados do beneficiário e do pagador.
    /// O número de controle da CIP, exigido no pagamento de boletos de outros bancos, fica em
    /// <see cref="MovimentoItemPagamentoTituloCodigoBarra.IdentificadorConsulta"/>. Boleto que o banco recusa
    /// (vencido, já pago, bloqueado...) chega como <see cref="Core.Errors.BancoApiException"/>.</para>
    ///
    /// <para><b>Pagamento</b>: o <c>transactionId</c> (até 9 dígitos, único) vem de
    /// <see cref="Movimento.NumeroDocumento"/> e é o que identifica o pagamento depois — fica em
    /// <see cref="Movimento.NumeroDocumentoNoBanco"/> e serve para consultar e cancelar. O banco recusa um
    /// <c>transactionId</c> repetido, então reenviar o mesmo movimento não duplica o pagamento. Boleto a vencer
    /// pago em dia não útil é agendado para o próximo dia útil (<see cref="BancoBrSituacaoEnum.Agendado"/>);
    /// a liquidação é confirmada por <see cref="ConsultarComprovantePorIdAsync"/>.</para>
    ///
    /// O portador do título informado no pagamento é o titular da conta pagadora (<c>ConfiguracaoAPI.Correntista</c>).
    /// A API não tem comprovante em PDF, consulta por idempotência nem DDA.
    /// </summary>
    internal class PagamentoBoletoClient : PagamentoBoletoApiBase
    {
        private const int CodigoBancoBradesco = 237;
        private const string Recurso = "boleto/pagamento-cobranca/v1/";

        private readonly BradescoHttpGateway _gateway;
        private readonly string _baseUrl;
        private readonly Func<DateTime> _relogio;

        /// <param name="relogio">Hora de Brasília usada em dataMovimento/horaTransacao; por padrão, o relógio local.</param>
        internal PagamentoBoletoClient(HttpClient httpClient, IAccessTokenProvider tokenProvider, Correntista origem, Uri baseUrl = null, Func<DateTime> relogio = null)
            : base(httpClient, origem)
        {
            _gateway = new BradescoHttpGateway(httpClient, tokenProvider);
            _baseUrl = BradescoHttpGateway.NormalizarBase(baseUrl ?? BradescoHttpGateway.PagamentosBaseUrl) + Recurso;
            _relogio = relogio ?? (() => DateTime.Now);
        }

        #region ::. Operações .::

        internal override async Task<Movimento> ConsultarBoletoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            ValidarOrigem();
            var entrada = EntradaTitulo(item);

            var titulo = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "cobranca-valida-titulo-pagamento/validaTituloPagamento", new JObject
            {
                ["tipoEntrada"] = entrada.Tipo,
                ["dadosEntrada"] = entrada.Dados,
                ["bancoTitulo"] = entrada.BancoTitulo,
                ["agenciaDeb"] = Origem.NumeroAgencia,
                ["contaDeb"] = Origem.NumeroConta,
            }, cancellationToken).ConfigureAwait(false);

            var codigoBarras = CodigoBarrasDe(titulo["codBarras"]) ?? CodigoBarrasDe(item.CodigoBarras);
            if (codigoBarras == null)
                throw new InvalidOperationException("O Bradesco não devolveu o código de barras do título consultado.");

            var vencimento = BradescoCampos.DataAAAAMMDD(titulo["dataVencimento"]);
            var valorTitulo = BradescoCampos.Decimal(titulo["valorTitulo"]) ?? movimento.ValorPagamento;
            var numeroCtrlCip = BradescoCampos.Texto(titulo["numeroCtrlCip"]);

            var agora = _relogio();
            var preEfetivacao = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "cobranca-pre-efetivacao/pre-efetivacao-pagamento", new JObject
            {
                ["agencia"] = Origem.NumeroAgencia,
                ["formaCaptura"] = 1,
                ["codigoBarras"] = codigoBarras,
                ["dataVencimento"] = vencimento.HasValue ? BradescoCampos.Data(vencimento.Value) : 0,
                ["valorTitulo"] = valorTitulo,
                ["dataMovimento"] = BradescoCampos.Data(agora),
                ["dataPagamento"] = BradescoCampos.Data(DataPagamento(movimento, agora)),
                ["horaTransacao"] = BradescoCampos.Hora(agora),
                ["numeroAutenticacao"] = "",
                ["formaPagamento"] = 2,
                ["bancoDebito"] = BancoDebito(),
                ["agenciaDebito"] = Origem.NumeroAgencia,
                ["digitoAgencia"] = DigitoAgencia(),
                ["contaDebito"] = Origem.NumeroConta,
                ["digitoConta"] = (Origem.DVConta ?? string.Empty).Trim(),
                ["linhaExtrato"] = BradescoCampos.Truncar(item.DescricaoObservacao, 40),
                ["numeroCtrlCip"] = numeroCtrlCip ?? "",
            }, cancellationToken).ConfigureAwait(false);

            AplicarConsulta(movimento, item, titulo, preEfetivacao, codigoBarras, numeroCtrlCip, vencimento, valorTitulo);
            return movimento;
        }

        /// <summary>
        /// Efetiva o pagamento. Os dados da consulta (vencimento, valor do título e número de controle da CIP)
        /// vêm do próprio movimento quando <see cref="ConsultarBoletoAsync"/> já os preencheu; do contrário a
        /// consulta é feita aqui.
        /// </summary>
        internal override async Task<Movimento> PagarBoletoAsync(Movimento movimento, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            ValidarOrigem();
            var transactionId = TransactionId(movimento);
            var portador = Portador();

            if (item.DataVencimento == null || item.ValorCodigoBarra <= 0 || CodigoBarrasDe(item.CodigoBarras) == null)
                await ConsultarBoletoAsync(movimento, cancellationToken).ConfigureAwait(false);

            var agora = _relogio();
            var resposta = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "cobranca-efetivacao/solicitacao/executar", new JObject
            {
                ["codigoAgencia"] = Origem.NumeroAgencia,
                ["formaCaptura"] = 1,
                ["codigoBarras"] = CodigoBarrasDe(item.CodigoBarras),
                ["dataVencimento"] = BradescoCampos.Data(item.DataVencimento.Value),
                ["valorTitulo"] = item.ValorCodigoBarra,
                ["dataMovimento"] = BradescoCampos.Data(agora),
                ["dataPagamento"] = BradescoCampos.Data(DataPagamento(movimento, agora)),
                ["horaTransacao"] = BradescoCampos.Hora(agora),
                ["numeroAutenticacao"] = "",
                ["formaPagamento"] = 2,
                ["bancoDebito"] = BancoDebito(),
                ["agenciaDebito"] = Origem.NumeroAgencia,
                ["digitoAgencia"] = DigitoAgencia(),
                ["contaDebito"] = Origem.NumeroConta,
                ["digitoConta"] = (Origem.DVConta ?? string.Empty).Trim(),
                ["linhaExtrato"] = BradescoCampos.Truncar(item.DescricaoObservacao, 40),
                ["indicadorFuncao"] = "1",
                ["numeroCtrlCip"] = item.IdentificadorConsulta ?? "",
                ["transactionId"] = transactionId,
                ["tipoIntermediador"] = "D",
                ["cnpjCpfPtdor"] = portador.Raiz,
                ["filialCnpjPtdor"] = portador.Filial,
                ["ctrlCnpjPtdor"] = portador.Controle,
            }, cancellationToken).ConfigureAwait(false);

            movimento.NumeroDocumentoNoBanco = transactionId.ToString(CultureInfo.InvariantCulture);

            var protocolo = BradescoCampos.Inteiro(resposta["nroProtocolo"]);
            if (protocolo.GetValueOrDefault() > 0)
                item.NumeroAutenticacaoPagamento = protocolo.Value.ToString(CultureInfo.InvariantCulture);

            // dataQuitacao é a data em que o banco vai liquidar (já considerando fim de semana e feriado).
            var quitacao = BradescoCampos.DataAAAAMMDD(resposta["dataQuitacao"]);
            var dataLiquidacao = quitacao ?? DataPagamento(movimento, agora);
            movimento.Situacao = dataLiquidacao.Date > agora.Date ? BancoBrSituacaoEnum.Agendado : BancoBrSituacaoEnum.Efetivado;
            movimento.DetalheRejeicao = null;
            if (quitacao.HasValue) movimento.Horario = quitacao.Value;

            var valorCobrado = BradescoCampos.Decimal(resposta["valorCobrado"]);
            if (movimento.ValorPagamento == 0 && valorCobrado.GetValueOrDefault() != 0)
                movimento.ValorPagamento = valorCobrado.Value;

            return movimento;
        }

        internal override async Task<Movimento> PagarBoletoComConsultaAsync(Movimento movimento, Guid idLancamento, CancellationToken cancellationToken = default)
        {
            await ConsultarBoletoAsync(movimento, cancellationToken).ConfigureAwait(false);
            if (movimento.Situacao == BancoBrSituacaoEnum.Rejeitado)
                return movimento;

            return await PagarBoletoAsync(movimento, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Consulta o pagamento pelo <c>transactionId</c> guardado em <see cref="Movimento.NumeroDocumentoNoBanco"/>.</summary>
        internal override async Task<Movimento> ConsultarComprovantePorIdAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            var item = ExtrairItem(movimento);
            ValidarOrigem();
            var transactionId = TransactionIdNoBanco(movimento);

            var resposta = await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "cobranca-pagamento-consulta/consulta-pagamento-especifico", new JObject
            {
                ["versaoLayout"] = "001",
                ["numeroBanco"] = BancoDebito(),
                ["numeroAgencia"] = Origem.NumeroAgencia,
                ["numeroConta"] = Origem.NumeroConta,
                ["numeroProtocolo"] = 0,
                ["dataPagamento"] = 0,
                ["transactionId"] = transactionId,
            }, cancellationToken).ConfigureAwait(false);

            AplicarSituacao(movimento, item, resposta);
            return movimento;
        }

        /// <summary>Exclui o agendamento (boleto a vencer, ainda não enviado para liquidação) pelo <c>transactionId</c>.</summary>
        internal override async Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, CancellationToken cancellationToken = default)
        {
            ExtrairItem(movimento);
            ValidarOrigem();
            var transactionId = TransactionIdNoBanco(movimento);

            await _gateway.SendAsync(HttpMethod.Post, _baseUrl + "cobranca-excluir-agendamento/exclusao/executar", new JObject
            {
                ["versao"] = 2,
                ["bancoDebito"] = BancoDebito(),
                ["agenciaDebito"] = Origem.NumeroAgencia,
                ["digitoAgencia"] = DigitoAgencia(),
                ["contaDebito"] = Origem.NumeroConta,
                ["digitoConta"] = (Origem.DVConta ?? string.Empty).Trim(),
                ["numeroProtocolo"] = 0,
                ["idTransacao"] = transactionId,
            }, cancellationToken).ConfigureAwait(false);

            movimento.Situacao = BancoBrSituacaoEnum.Cancelado;
            movimento.DetalheRejeicao = "Agendamento cancelado.";
            return movimento;
        }

        internal override async Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<(Movimento Movimento, Guid IdLancamento)> itens, CancellationToken cancellationToken = default)
        {
            if (itens == null) throw new ArgumentNullException(nameof(itens));

            var resultados = new List<PagamentoBoletoLoteResultadoItem>();
            foreach (var item in itens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var movimento = await PagarBoletoComConsultaAsync(item.Movimento, item.IdLancamento, cancellationToken).ConfigureAwait(false);
                    resultados.Add(PagamentoBoletoLoteResultadoItem.ComSucesso(movimento, item.IdLancamento));
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    resultados.Add(PagamentoBoletoLoteResultadoItem.ComFalha(item.Movimento, item.IdLancamento, ex));
                }
            }

            return resultados;
        }

        internal override Task<Movimento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de boletos do Bradesco não consulta pagamentos por chave de idempotência: use o transactionId (Movimento.NumeroDocumento).");

        internal override Task<IReadOnlyList<Movimento>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A API de boletos do Bradesco não oferece consulta de DDA.");

        #endregion

        #region ::. Mapeamento Movimento <-> Bradesco .::

        private static MovimentoItemPagamentoTituloCodigoBarra ExtrairItem(Movimento movimento)
        {
            if (movimento == null) throw new ArgumentNullException(nameof(movimento));
            return movimento.MovimentoItem as MovimentoItemPagamentoTituloCodigoBarra
                ?? throw new ArgumentException("Informe um MovimentoItemPagamentoTituloCodigoBarra em Movimento.MovimentoItem.", nameof(movimento));
        }

        private void ValidarOrigem()
        {
            if (Origem == null || Origem.NumeroAgencia <= 0 || Origem.NumeroConta <= 0)
                throw new InvalidOperationException("O Bradesco exige agência e conta da conta pagadora (ConfiguracaoAPI.Correntista).");
        }

        private int BancoDebito() => Origem.Banco != 0 ? Origem.Banco : CodigoBancoBradesco;

        private int DigitoAgencia() => int.TryParse(Origem.DVAgencia, NumberStyles.None, CultureInfo.InvariantCulture, out var digito) ? digito : 0;

        private static DateTime DataPagamento(Movimento movimento, DateTime agora) =>
            movimento.DataPagamento == default(DateTime) ? agora.Date : movimento.DataPagamento.Date;

        /// <summary>O transactionId do pagamento: numérico, até 9 dígitos, único e imutável por pagamento.</summary>
        private static long TransactionId(Movimento movimento)
        {
            var numero = movimento.NumeroDocumento?.Trim();
            if (string.IsNullOrEmpty(numero) || numero.Length > 9 || !long.TryParse(numero, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 1)
                throw new ArgumentException("O Bradesco exige em Movimento.NumeroDocumento um identificador numérico de até 9 dígitos (transactionId), único por pagamento. Preserve-o nos reenvios.", nameof(movimento));
            return id;
        }

        private static long TransactionIdNoBanco(Movimento movimento)
        {
            if (!long.TryParse(movimento.NumeroDocumentoNoBanco?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id < 1)
                throw new InvalidOperationException("Informe em NumeroDocumentoNoBanco o transactionId do pagamento (devolvido por PagarBoletoAsync).");
            return id;
        }

        private (long Raiz, int Filial, int Controle) Portador()
        {
            if (string.IsNullOrWhiteSpace(Origem.CPF_CNPJ))
                throw new InvalidOperationException("O Bradesco exige o CPF/CNPJ do portador do título: informe o do titular da conta pagadora (ConfiguracaoAPI.Correntista).");
            return BradescoCampos.DividirDocumento(Origem.CPF_CNPJ);
        }

        /// <summary>
        /// Código de barras com 44 dígitos; o banco pode replicá-lo com zeros à esquerda (campo de 54 posições),
        /// por isso só as últimas 44 posições contam.
        /// </summary>
        private static string CodigoBarrasDe(JToken token) => CodigoBarrasDe(BradescoCampos.Texto(token));

        private static string CodigoBarrasDe(string texto)
        {
            var digitos = BradescoCampos.Digitos(texto);
            if (digitos.Length > 44) digitos = digitos.Substring(digitos.Length - 44);
            return digitos.Length == 44 ? digitos : null;
        }

        /// <summary>Código de barras (tipo 1, com zeros à esquerda até 54 posições) ou linha digitável (tipo 2, com máscara).</summary>
        private static (int Tipo, string Dados, int BancoTitulo) EntradaTitulo(MovimentoItemPagamentoTituloCodigoBarra item)
        {
            var barras = CodigoBarrasDe(item.CodigoBarras);
            if (barras != null)
                return (1, barras.PadLeft(54, '0'), int.Parse(barras.Substring(0, 3), CultureInfo.InvariantCulture));

            var linha = BradescoCampos.Digitos(item.NumeroLinhaDigitavel);
            if (linha.Length == 47)
                return (2, $"{linha.Substring(0, 5)}.{linha.Substring(5, 5)} {linha.Substring(10, 5)}.{linha.Substring(15, 6)} {linha.Substring(21, 5)}.{linha.Substring(26, 6)} {linha.Substring(32, 1)} {linha.Substring(33, 14)}",
                    int.Parse(linha.Substring(0, 3), CultureInfo.InvariantCulture));

            throw new InvalidOperationException("Informe o código de barras (44 dígitos) ou a linha digitável (47 dígitos) do boleto.");
        }

        private static void AplicarConsulta(Movimento movimento, MovimentoItemPagamentoTituloCodigoBarra item, JObject titulo, JObject pre, string codigoBarras, string numeroCtrlCip, DateTime? vencimento, decimal valorTitulo)
        {
            item.CodigoBarras = codigoBarras;
            item.IdentificadorConsulta = numeroCtrlCip;
            item.ValorCodigoBarra = valorTitulo;
            item.DataVencimento = BradescoCampos.DataAAAAMMDD(pre["dataVctoTitlo"]) ?? vencimento;
            item.Desconto = Soma(pre, "valorDesconto", "valorAbatimento", "valorBonificacao");
            item.Acrescimo = Soma(pre, "valorMulta", "valorJuros");
            item.NomeInstituicaoEmissora = BradescoCampos.Texto(pre["nomeBancoBenef"]) ?? BradescoCampos.Texto(titulo["nmBanco"]);
            item.NomeFantasiaBeneficiario = BradescoCampos.Texto(pre["nomeFantasiaBenef"]) ?? BradescoCampos.Texto(titulo["nomeFantasiaBeneficiario"])
                ?? BradescoCampos.Texto(pre["nomeCedente"]) ?? BradescoCampos.Texto(titulo["nmCedente"]);
            item.PagadorConfirmadoNome = BradescoCampos.Texto(pre["nomePagador"]) ?? BradescoCampos.Texto(titulo["nomeSacado"]);
            AplicarPagador(item, BradescoCampos.Inteiro(pre["cnpjCpfSacado"]) ?? BradescoCampos.Inteiro(titulo["cnpjSacado"]));
            if (string.IsNullOrWhiteSpace(item.NumeroLinhaDigitavel))
                item.NumeroLinhaDigitavel = $"{BradescoCampos.Texto(pre["linhaDigitavel1"])} {BradescoCampos.Texto(pre["linhaDigitavel2"])}".Trim();

            // cindcdAltVlr: 1 = qualquer valor, 2 = entre mínimo e máximo, 3 = não aceita valor diferente, 4 = a partir do mínimo.
            var alteraValor = BradescoCampos.Texto(titulo["cindcdAltVlr"]) ?? BradescoCampos.Texto(titulo["pagamentoValorDiferentes"]);
            item.AceitaValorDivergente = item.PermiteAlterarValor = alteraValor != null && alteraValor != "3";

            var valorCobrado = BradescoCampos.Decimal(pre["valorCobrado"]);
            if (movimento.ValorPagamento == 0 && valorCobrado.GetValueOrDefault() != 0)
                movimento.ValorPagamento = valorCobrado.Value;

            movimento.Situacao = BancoBrSituacaoEnum.NaoIntegrado;
            movimento.DetalheRejeicao = null;
        }

        /// <summary>
        /// situacaoPagamento: 1 e 11 = efetuado/quitado; 7 = agendado; 2, 3, 8 e 9 = anulado/estornado;
        /// 12 = agendamento não quitado.
        /// </summary>
        private static void AplicarSituacao(Movimento movimento, MovimentoItemPagamentoTituloCodigoBarra item, JObject resposta)
        {
            var descricaoMotivo = BradescoCampos.Inteiro(resposta["motNaoEfet"]).GetValueOrDefault() != 0 ? BradescoCampos.Texto(resposta["descrMotivo"]) : null;

            switch (BradescoCampos.Inteiro(resposta["situacaoPagamento"]))
            {
                case 1:
                case 11:
                    movimento.Situacao = BancoBrSituacaoEnum.Efetivado;
                    break;

                case 7:
                    movimento.Situacao = BancoBrSituacaoEnum.Agendado;
                    break;

                case 2:
                case 3:
                case 8:
                case 9:
                    movimento.Situacao = BancoBrSituacaoEnum.Cancelado;
                    break;

                case 12:
                    movimento.Situacao = BancoBrSituacaoEnum.Rejeitado;
                    break;

                default:
                    movimento.Situacao = BancoBrSituacaoEnum.NaoIntegrado;
                    break;
            }
            movimento.DetalheRejeicao = movimento.Situacao == BancoBrSituacaoEnum.Rejeitado || movimento.Situacao == BancoBrSituacaoEnum.Cancelado ? descricaoMotivo : null;

            var valor = BradescoCampos.Decimal(resposta["valorPagamento"]);
            if (valor.GetValueOrDefault() > 0)
                movimento.ValorPagamento = valor.Value;

            var inclusao = BradescoCampos.DataAAAAMMDD(resposta["dataInclTit"]);
            if (inclusao.HasValue)
            {
                var hora = (int)BradescoCampos.Inteiro(resposta["horaTransacao"]).GetValueOrDefault();
                movimento.Horario = inclusao.Value.AddHours(hora / 10000).AddMinutes(hora / 100 % 100).AddSeconds(hora % 100);
            }

            item.DataVencimento = BradescoCampos.DataAAAAMMDD(resposta["dataVencimento"]) ?? item.DataVencimento;
            if (BradescoCampos.Decimal(resposta["valorTituloOritem"]) is decimal nominal && nominal > 0) item.ValorCodigoBarra = nominal;
            item.Desconto = Soma(resposta, "valorDescontoTitulo", "vabtmtTitloCalcd", "valorBonusTitulo");
            item.Acrescimo = Soma(resposta, "valorMultaTitulo", "valorJurosTitulo");
            item.NomeFantasiaBeneficiario = BradescoCampos.Texto(resposta["nomeFantasiaBeneficiario"]) ?? item.NomeFantasiaBeneficiario;
            item.PagadorConfirmadoNome = BradescoCampos.Texto(resposta["isacdo"]) ?? item.PagadorConfirmadoNome;
            AplicarPagador(item, BradescoCampos.Inteiro(resposta["ccgcCpfSacdo"]));
            if (string.IsNullOrWhiteSpace(item.NumeroLinhaDigitavel))
                item.NumeroLinhaDigitavel = $"{BradescoCampos.Texto(resposta["digitavel1"])} {BradescoCampos.Texto(resposta["digitavel2"])}".Trim();
        }

        /// <summary>O documento vem como número, sem os zeros à esquerda: recompõe CPF (11) ou CNPJ (14).</summary>
        private static void AplicarPagador(MovimentoItemPagamentoTituloCodigoBarra item, long? documento)
        {
            if (!documento.HasValue || documento.Value <= 0) return;

            var digitos = documento.Value.ToString(CultureInfo.InvariantCulture);
            var tamanho = digitos.Length <= 11 ? 11 : 14;
            item.PagadorConfirmadoDocumento = digitos.PadLeft(tamanho, '0');
            item.PagadorConfirmadoTipoPessoa = tamanho == 14 ? "J" : "F";
        }

        private static decimal Soma(JObject resposta, params string[] campos)
        {
            decimal total = 0;
            foreach (var campo in campos)
                total += BradescoCampos.Decimal(resposta[campo]).GetValueOrDefault();
            return total;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base
{
    /// <summary>
    /// Base comum para clientes da API de pagamento de boletos, independente do banco —
    /// espelha o papel de BancoBr.CNAB.Base.Banco no domínio de geração de CNAB: cada banco
    /// herda e implementa os métodos abaixo de acordo com sua própria API.
    ///
    /// O contrato público é o mesmo <see cref="Movimento"/>/<see cref="MovimentoItem"/> usado
    /// pelo BancoBr.CNAB (a operação de boleto espera um
    /// <see cref="MovimentoItemPagamentoTituloCodigoBarra"/> como
    /// <see cref="Movimento.MovimentoItem"/>): cada método recebe o movimento, mapeia para o
    /// formato da API do banco, e aplica a resposta de volta no mesmo objeto — que é então
    /// devolvido (mesma instância, mutada in-place). O <paramref name="origem"/> é a conta
    /// pagadora, separada do movimento pelo mesmo motivo que ArquivoCNAB recebe o
    /// <see cref="Correntista"/> separado da lista de movimentos.
    /// </summary>
    internal abstract class PagamentoBoletoApiBase
    {
        protected PagamentoBoletoApiBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected HttpClient HttpClient { get; }

        /// <summary>
        /// Consulta o boleto identificado por
        /// <see cref="MovimentoItemPagamentoTituloCodigoBarra.CodigoBarras"/>. Quando o banco
        /// não encontra o boleto ou bloqueia o pagamento, o motivo fica em
        /// <see cref="Movimento.SituacaoBancoBr"/> + <see cref="Movimento.DetalheRejeicaoBancoBr"/>.
        /// </summary>
        internal abstract Task<Movimento> ConsultarBoletoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga um boleto já consultado — exige o
        /// <see cref="MovimentoItemPagamentoTituloCodigoBarra.IdentificadorConsulta"/> devolvido
        /// pela consulta prévia.
        /// </summary>
        internal abstract Task<Movimento> PagarBoletoAsync(Movimento movimento, Correntista origem, string idempotencyKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Conveniência para o caso sem aprovação humana entre consulta e pagamento: consulta o
        /// boleto, e só se ele não estiver bloqueado, monta a requisição de pagamento (usando o
        /// IdentificadorConsulta retornado) e a envia. Quando o ERP precisa exibir o valor para
        /// confirmação antes de pagar, use ConsultarBoletoAsync e PagarBoletoAsync separadamente.
        /// </summary>
        internal abstract Task<Movimento> PagarBoletoComConsultaAsync(Movimento movimento, Correntista origem, Guid idLancamento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Paga vários boletos em sequência (bancos sem endpoint de lote geram uma consulta + um
        /// pagamento como chamadas HTTP separadas, throttladas pelo rate limit já configurado no
        /// cliente). Um item com erro não interrompe os demais.
        /// </summary>
        internal abstract Task<IReadOnlyList<PagamentoBoletoLoteResultadoItem>> PagarLoteBoletosAsync(IEnumerable<(Movimento Movimento, Guid IdLancamento)> itens, Correntista origem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta o comprovante do pagamento cujo identificador no banco está em
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/>.
        /// </summary>
        internal abstract Task<Movimento> ConsultarComprovantePorIdAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancela o agendamento do pagamento cujo identificador no banco está em
        /// <see cref="Movimento.NumeroDocumentoNoBanco"/>.
        /// </summary>
        internal abstract Task<Movimento> CancelarAgendamentoAsync(Movimento movimento, Correntista origem, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recupera o comprovante de um pagamento pela idempotency key usada no envio. Como não
        /// há um movimento de entrada (o cenário típico é recuperar um pagamento cujo retorno se
        /// perdeu), devolve um <see cref="Movimento"/> novo montado a partir do comprovante.
        /// </summary>
        internal abstract Task<Movimento> ConsultarComprovantePorIdempotencyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consulta de lista (boletos DDA com vencimento/pagamento num intervalo) — não parte de
        /// um único <see cref="Movimento"/> de entrada, mas cada item devolvido vira um
        /// <see cref="Movimento"/> com <see cref="MovimentoItemDDA"/>, como qualquer outra
        /// operação.
        /// </summary>
        internal abstract Task<IReadOnlyList<Movimento>> ConsultarBoletosDdaAsync(long numeroConta, DateTime dataInicial, DateTime dataFinal, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken cancellationToken = default);
    }
}

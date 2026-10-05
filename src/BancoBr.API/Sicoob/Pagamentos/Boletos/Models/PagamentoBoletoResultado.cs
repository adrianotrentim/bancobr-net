namespace BancoBr.API.Sicoob.Pagamentos.Boletos.Models
{
    /// <summary>
    /// Resultado de um pagamento de boleto. Cobre tanto o retorno direto de
    /// POST /boletos/pagamentos/{codigoBarras} (200 com comprovante, 202 pendência de
    /// assinatura, 204 sem corpo) quanto os desvios possíveis no fluxo de duas etapas de
    /// <see cref="PagamentoBoletoClient.PagarBoletoComConsultaAsync"/> (boleto não encontrado
    /// na consulta, ou pagamento bloqueado pelo Sicoob) — representados explicitamente em vez
    /// de sobrecarregar um Comprovante nulo com múltiplos significados.
    /// </summary>
    internal class PagamentoBoletoResultado
    {
        public bool PendenteAssinatura { get; }

        public bool BoletoNaoEncontrado { get; }

        public bool PagamentoBloqueado { get; }

        public string MensagemBloqueio { get; }

        public ComprovantePagamento Comprovante { get; }

        private PagamentoBoletoResultado(bool pendenteAssinatura, bool boletoNaoEncontrado, bool pagamentoBloqueado, string mensagemBloqueio, ComprovantePagamento comprovante)
        {
            PendenteAssinatura = pendenteAssinatura;
            BoletoNaoEncontrado = boletoNaoEncontrado;
            PagamentoBloqueado = pagamentoBloqueado;
            MensagemBloqueio = mensagemBloqueio;
            Comprovante = comprovante;
        }

        public static PagamentoBoletoResultado Efetivado(ComprovantePagamento comprovante) =>
            new PagamentoBoletoResultado(pendenteAssinatura: false, boletoNaoEncontrado: false, pagamentoBloqueado: false, mensagemBloqueio: null, comprovante);

        public static PagamentoBoletoResultado PendenteDeAssinatura() =>
            new PagamentoBoletoResultado(pendenteAssinatura: true, boletoNaoEncontrado: false, pagamentoBloqueado: false, mensagemBloqueio: null, comprovante: null);

        public static PagamentoBoletoResultado SemConteudo() =>
            new PagamentoBoletoResultado(pendenteAssinatura: false, boletoNaoEncontrado: false, pagamentoBloqueado: false, mensagemBloqueio: null, comprovante: null);

        public static PagamentoBoletoResultado NaoEncontrado() =>
            new PagamentoBoletoResultado(pendenteAssinatura: false, boletoNaoEncontrado: true, pagamentoBloqueado: false, mensagemBloqueio: null, comprovante: null);

        public static PagamentoBoletoResultado Bloqueado(string mensagemBloqueio) =>
            new PagamentoBoletoResultado(pendenteAssinatura: false, boletoNaoEncontrado: false, pagamentoBloqueado: true, mensagemBloqueio, comprovante: null);
    }
}

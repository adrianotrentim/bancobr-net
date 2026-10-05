namespace BancoBr.API.Sicoob.Pagamentos.Convenios.Models
{
    /// <summary>
    /// Resultado de um pagamento de convênio. Cobre o retorno de
    /// POST /arrecadacao/codigo-barras/{codigoBarras}/pagamentos (200 com comprovante, ou 202
    /// quando a transação está pendente de assinatura) — diferente de boleto, este endpoint não
    /// tem um fluxo de duas etapas (consulta + pagamento) embutido no cliente.
    /// </summary>
    internal class PagamentoConvenioResultado
    {
        public bool PendenteAssinatura { get; }

        public ArrecadacaoResultado Resultado { get; }

        private PagamentoConvenioResultado(bool pendenteAssinatura, ArrecadacaoResultado resultado)
        {
            PendenteAssinatura = pendenteAssinatura;
            Resultado = resultado;
        }

        public static PagamentoConvenioResultado Efetivado(ArrecadacaoResultado resultado) =>
            new PagamentoConvenioResultado(pendenteAssinatura: false, resultado);

        public static PagamentoConvenioResultado PendenteDeAssinatura() =>
            new PagamentoConvenioResultado(pendenteAssinatura: true, resultado: null);
    }
}

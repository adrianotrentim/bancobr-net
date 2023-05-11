using System.ComponentModel;

namespace BancoBr.Common.Enums
{
    public enum CodigoInstrucaoMovimentoEnum
    {
        [Description("00 - Inclusão de Pagamento Liberado")]
        InclusaoRegistroDetalheLiberado = 0,
        [Description("09 - Inclusão de Pagamento Bloqueado - Pendente de Liberação no Banco")]
        InclusaoRegistroDetalheBloqueado = 9,
        [Description("10 - Alteração de Pagamento - Liberado para Bloqueado")]
        AlteracaoPagamentoLiberadoParaBloqueio = 10,
        [Description("11 - Alteração de Pagamento - Bloqueado para Liberado")]
        AlteracaoPagamentoBloqueadoParaLiberacao = 11,
        [Description("17 - Alteração de Pagamento - Valor do Título")]
        AlteracaoValorTitulo = 17,
        [Description("19 - Alteração de Pagamento - Data de Pagamento")]
        AlteracaoDataPagamento = 19,
        [Description("23 - Baixa do Pagamento - Não Pagar")]
        PagamentoDiretoFornecedor_Baixar = 23,
        [Description("25 - Manutenção da Carteira - Não Pagar")]
        ManutencaoCarteira_NaoPagar = 25,
        [Description("27 - Retirada da Carteira - Não Pagar")]
        RetiradaCarteira_NaoPagar = 27,
        [Description("33 - Estorno por Devolução")]
        EstornoPorDevolucao = 33, // Somente para Tipo de Movimento de Retorno
        [Description("40 - Alegação do Pagador")]
        AlegacaoPagador = 40, // Somente para Tipo de Movimento de Retorno
        [Description("99 - Exclusão de Pagamento")]
        ExclusaoRegistroDetalheIncluidoAnteriormente = 99
    }

    public enum AvisoFavorecidoEnum
    {
        NaoEmiteAviso = 0
    }
}
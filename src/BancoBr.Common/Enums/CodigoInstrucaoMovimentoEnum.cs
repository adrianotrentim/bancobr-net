namespace BancoBr.Common.Enums
{
    public enum CodigoInstrucaoMovimentoEnum
    {
        InclusaoRegistroDetalheLiberado = 0,
        InclusaoRegistroDetalheBloqueado = 9,
        AlteracaoPagamentoLiberadoParaBloqueio = 10,
        AlteracaoPagamentoBloqueadoParaLiberacao = 11,
        AlteracaoValorTitulo = 17,
        AlteracaoDataPagamento = 19,
        PagamentoDiretoFornecedor_Baixar = 23,
        ManutencaoCarteira_NaoPagar = 25,
        RetiradaCarteira_NaoPagar = 27,
        EstornoPorDevolucao = 33, // Somente para Tipo de Movimento 3
        AlegacaoPagador = 40,
        ExclusaoRegistroDetalheIncluidoAnteriormente = 99
    }

    public enum AvisoFavorecidoEnum
    {
        NaoEmiteAviso = 0
    }
}
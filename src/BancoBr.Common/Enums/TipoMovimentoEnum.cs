namespace BancoBr.Common.Enums
{
    public enum TipoMovimentoEnum
    {
        Inclusao = 0,
        Consulta = 1,
        Suspensao = 2,
        Estorno = 3, //Somente para Retorno
        Reativacao = 4,
        Alteracao = 5,
        Liquidacao = 7,
        Exclusao = 9
    }
}
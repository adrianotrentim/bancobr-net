using System.ComponentModel;

namespace BancoBr.Common.Enums
{
    public enum TipoMovimentoEnum
    {
        [Description("Inclusão")]
        Inclusao = 0,
        [Description("Consulta")]
        Consulta = 1,
        [Description("Suspensão")]
        Suspensao = 2,
        [Description("Estorno")]
        Estorno = 3, //Somente para Retorno
        [Description("Reativação")]
        Reativacao = 4,
        [Description("Alteração")]
        Alteracao = 5,
        [Description("Liquidação")]
        Liquidacao = 7,
        [Description("Exclusão")]
        Exclusao = 9
    }
}
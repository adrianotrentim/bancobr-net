using System.Runtime.CompilerServices;

namespace BancoBr.API.Core.Operacoes
{
    /// <summary>Estado auxiliar da integração, sem alterar os modelos compartilhados com CNAB.</summary>
    internal sealed class MetadadosOperacao
    {
        private static readonly ConditionalWeakTable<object, MetadadosOperacao> Valores = new ConditionalWeakTable<object, MetadadosOperacao>();
        internal static MetadadosOperacao De(object alvo) => Valores.GetValue(alvo, _ => new MetadadosOperacao());
        internal string IdRequisicao { get; set; }
        internal string IdentidadeConsulta { get; set; }
        internal string SituacaoOriginal { get; set; }
        internal decimal? SaldoAposLancamento { get; set; }
    }
}

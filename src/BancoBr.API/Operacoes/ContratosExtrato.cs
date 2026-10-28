using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.API.Operacoes
{
    /// <summary>Período do extrato, usando a conta configurada conforme o contrato de cada banco.</summary>
    public sealed class RequisicaoExtrato
    {
        public long NumeroContaCorrente { get; set; }
        public int Mes { get; set; }
        public int Ano { get; set; }
        public int? DiaInicial { get; set; }
        public int? DiaFinal { get; set; }
        public bool AgruparCnab { get; set; }
    }

    /// <summary>Lançamento com saldo após a movimentação, quando disponibilizado pelo banco.</summary>
    public sealed class LancamentoExtrato
    {
        public Movimento Movimento { get; internal set; }
        public decimal? SaldoAposLancamento { get; internal set; }
    }

    /// <summary>Extrato completo: os mesmos movimentos da chamada antiga, dados adicionais e respostas originais de todas as páginas.</summary>
    public sealed class ResultadoExtrato
    {
        public IReadOnlyList<Movimento> Movimentos { get; internal set; }
        public IReadOnlyList<LancamentoExtrato> Lancamentos { get; internal set; }
        public IReadOnlyList<RetornoBancario> RetornosBanco { get; internal set; }
    }
}

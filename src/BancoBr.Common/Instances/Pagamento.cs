using BancoBr.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using BancoBr.Common.Enums;

namespace BancoBr.Common.Instances
{
    public class Pagamento
    {
        public Pagamento()
        {
            TipoMovimento = TipoMovimentoEnum.Inclusao;
            Moeda = "BRL";
            QuantidadeMoeda = 0;
        }

        public Pessoa EmpresaCedente { get; set; }
        public Pessoa PessoaEmpresaDestino { get; set; }
        public TipoMovimentoEnum TipoMovimento { get; set; }
        public int NumeroDocumento { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Moeda { get; set; }
        public int QuantidadeMoeda { get; set; }
        public decimal ValorPagamento { get; set; }
        public string FinalidadeLancamento { get; set; }
    }
}

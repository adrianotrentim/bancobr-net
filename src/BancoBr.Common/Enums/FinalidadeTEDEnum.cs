using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BancoBr.Common.Enums
{
    public enum FinalidadeTEDEnum
    {
        [Description("0 - Não se Aplica")]
        NaoAplicavel = 0,
        [Description("001 - Pagamento de Impostos, Tributos e Taxas")]
        PagamentoImpostosTributosTaxas = 1,
        [Description("002 - Pagamento à Concessionárias de Serviço Público")]
        PagamentoConcessionariaServicoPublico = 2,
        [Description("003 - Pagamentos de Dividendos")]
        PagamentoDividendos = 3,
        [Description("004 - Pagamento de Salários")]
        PagamentoSalarios = 4,
        [Description("005 - Pagamento de Fornecedores")]
        PagamentoFornecedores = 5,
        [Description("006 - Pagamento de Honorários")]
        PagamentoHonorarios = 6,
        [Description("007 - Pagamento de Aluguéis e Taxas de Condomínio")]
        PagamentoAluguel = 7,
        [Description("008 - Pagamento de Duplicatas e Títulos")]
        PagamentoDuplicatasTitulos = 8,
        [Description("009 - Pagamento de Mensalidade Escolar")]
        PagamentoMensalidadeEscolar = 9,
        [Description("010 - Crédito em Conta")]
        CreditoEmConta = 10,
        [Description("011 - Pagamento a Corretoras")]
        PagamentoCorretoras = 11,
        [Description("101 - Pensão Alimentícia")]
        PensaoALimenticia = 101,
    }
}

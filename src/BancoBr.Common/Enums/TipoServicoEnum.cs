using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BancoBr.Common.Enums
{
    public enum TipoServicoEnum
    {
        [Description("Cobrança")]
        Cobranca = 1,
        [Description("Bloqueto Eletrônico")]
        BloquetoEletronico = 3,
        [Description("Conciliação Bancária")]
        ConciliacaoBancaria = 4,
        [Description("Débitos")]
        Debitos = 5,
        [Description("Custódia de Cheques")]
        CustodiaCheques = 6,
        [Description("Gestão de Caixa")]
        GestaoCaixa = 7,
        [Description("Consulta Margem")]
        ConsultaMargem = 8,
        [Description("Averbação de Consignados")]
        AverbacaoConsignacao = 9,
        [Description("Pagamento de Dividendos")]
        PagamentoDividentos = 10,
        [Description("Manutenção de Consignação")]
        ManutencaoConsignacao = 11,
        [Description("Consignação de Parcela")]
        ConsignacaoParcela = 12,
        [Description("Glosa de Consignação")]
        GlosaConsignacao = 13,
        [Description("Consulta de Tributos")]
        ConsultaTributos = 14,
        [Description("Pagamento de Fornecedores")]
        PagamentoFornecedor = 20,
        [Description("Pagamento de Tributos")]
        Tributos = 22,
        [Description("Compror")]
        Compror = 25,
        [Description("Compror Rotativo")]
        ComprorRotativo = 26,
        [Description("Alegação do Sacado")]
        AlegacaoSacado = 29,
        [Description("Pagamento de Salários")]
        PagamentoSalarios = 30,
        [Description("Vendor")]
        Vendor = 40,
        [Description("Vendor a Termo")]
        VendorTermo = 41,
        [Description("Pagamento de Sinistros")]
        PagamentoSinitro = 50,
        [Description("Despesas de Viagens")]
        DespesaViagem = 60,
        [Description("Pagamento de Representantes / Vendedores")]
        PagamentoRepresentanteVendedor = 80,
        [Description("Pagamento de Benefícios")]
        PagamentoBeneficio = 90,
        [Description("Pagamento Diversos")]
        PagamentoDiverso = 98
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BancoBr.Common.Enums
{
    public enum TipoServicoEnum
    {
        [Description("01 - Cobrança")]
        Cobranca = 1,
        [Description("03 - Bloqueto Eletrônico")]
        BloquetoEletronico = 3,
        [Description("04 - Conciliação Bancária")]
        ConciliacaoBancaria = 4,
        [Description("05 - Débitos")]
        Debitos = 5,
        [Description("06 - Custódia de Cheques")]
        CustodiaCheques = 6,
        [Description("07 - Gestão de Caixa")]
        GestaoCaixa = 7,
        [Description("08 - Consulta Margem")]
        ConsultaMargem = 8,
        [Description("09 - Averbação de Consignados")]
        AverbacaoConsignacao = 9,
        [Description("10 - Pagamento de Dividendos")]
        PagamentoDividentos = 10,
        [Description("11 - Manutenção de Consignação")]
        ManutencaoConsignacao = 11,
        [Description("12 - Consignação de Parcela")]
        ConsignacaoParcela = 12,
        [Description("13 - Glosa de Consignação")]
        GlosaConsignacao = 13,
        [Description("14 - Consulta de Tributos")]
        ConsultaTributos = 14,
        [Description("20 - Pagamento de Fornecedores")]
        PagamentoFornecedor = 20,
        [Description("22 - Pagamento de Tributos")]
        Tributos = 22,
        [Description("25 - Compror")]
        Compror = 25,
        [Description("26 - Compror Rotativo")]
        ComprorRotativo = 26,
        [Description("29 - Alegação do Sacado")]
        AlegacaoSacado = 29,
        [Description("30 - Pagamento de Salários")]
        PagamentoSalarios = 30,
        [Description("40 - Vendor")]
        Vendor = 40,
        [Description("41 - Vendor a Termo")]
        VendorTermo = 41,
        [Description("50 - Pagamento de Sinistros")]
        PagamentoSinitro = 50,
        [Description("60 - Despesas de Viagens")]
        DespesaViagem = 60,
        [Description("80 - Pagamento de Representantes / Vendedores")]
        PagamentoRepresentanteVendedor = 80,
        [Description("90 - Pagamento de Benefícios")]
        PagamentoBeneficio = 90,
        [Description("98 - Pagamento Diversos")]
        PagamentoDiverso = 98
    }
}

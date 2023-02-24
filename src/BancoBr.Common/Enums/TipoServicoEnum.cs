using System;
using System.Collections.Generic;
using System.Text;

namespace BancoBr.Common.Enums
{
    public enum TipoServicoEnum
    {
        Cobranca = 1,
        BloquetoEletronico = 3,
        ConciliacaoBancaria = 4,
        Debitos = 5,
        CustodiaCheques = 6,
        GestaoCaixa = 7,
        CoinsultaMargem = 8,
        AverbacaoConsignacao = 9,
        PagamentoDividentos = 10,
        ManutencaoConsignacao = 11,
        ConsignacaoParcela = 12,
        GlosaConsignacao = 13,
        ConsultaTributos = 14,
        PagamentoFornecedor = 20,
        Tributos = 22,
        Compror = 25,
        ComprorRotativo = 26,
        AlegacaoSacado = 29,
        PagamentoSalarios = 30,
        Vendor = 40,
        VendorTermo = 41,
        PagamentoSinitro = 50,
        DespesaViagem = 60,
        PagamentoRepresentanteVendedor = 80,
        PagamentoBeneficio = 90,
        PagamentoDiverso = 98
    }
}

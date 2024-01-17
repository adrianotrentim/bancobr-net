using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BancoBr.Common.Enums
{
    public enum TipoServicoEnum
    {
        [Description("03 - Bloqueto Eletrônico")]
        BloquetoEletronico = 3,
        [Description("20 - Pagamento de Fornecedores")]
        PagamentoFornecedor = 20,
        [Description("22 - Pagamento de Tributos")]
        Tributos = 22,
        [Description("30 - Pagamento de Salários")]
        PagamentoSalarios = 30,
        [Description("98 - Pagamento Diversos")]
        PagamentoDiverso = 98
    }
}

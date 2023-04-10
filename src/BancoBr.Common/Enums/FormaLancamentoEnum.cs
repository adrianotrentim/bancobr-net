using System.ComponentModel;

namespace BancoBr.Common.Enums
{
    public enum FormaLancamentoEnum
    {
        [Description("01 - Crédito em Conta Corrente - Mesmo Banco")]
        CreditoContaMesmoBanco = 1,
        [Description("02 - Cheque de Pagamento")]
        ChequePagamento = 2,
        [Description("03 - DOC / TED")]
        DOC_TED = 3,
        [Description("04 - Cartão Salário")]
        CartaoSalario = 4, //Somente para tipo de Serviço = 30
        [Description("05 - Crédito Conta Poupança - Mesmo Banco")]
        CreditoContaPoupancaMesmoBanco = 5,
        [Description("10 - O.P. Disposição")]
        OPDisposicao = 10,
        [Description("11 - Trib. Código de Barras")]
        PagamentoTributosCodigoBarra = 11,
        [Description("16 - DARF Normal")]
        TributoDARFNormal = 16,
        [Description("17 - GPS")]
        TributoGPS = 17,
        [Description("18 - DARF Simples")]
        TributoDARFSimples = 18,
        [Description("19 - IPTU")]
        TributoIPTU = 19,
        [Description("20 - Pagamento Autenticado")]
        PagamentoAutenticado = 20,
        [Description("21 - DARJ")]
        TributoDARJ = 21,
        [Description("22 - GARE SP - ICMS")]
        TributoGARE_SP_ICMS = 22,
        [Description("23 - GARE SP - DR")]
        TributoGARE_SP_DR = 23,
        [Description("24 - GARE SP - ITCMD")]
        TributoGARE_SP_ITCMD = 24,
        [Description("25 - IPVA")]
        TributoIPVA = 25,
        [Description("26 - Licenciamento de Veículos")]
        TributoLicenciamento = 26,
        [Description("27 - DPVAT")]
        TributoDPVAT = 27,
        [Description("30 - Liquidação Título - Mesmo Banco")]
        LiquidacaoProprioBanco = 30,
        [Description("31 - Pagamento Título - Outro Banco")]
        PagamentoTituloOutroBanco = 31,
        [Description("40 - Extrato Conta Corrente")]
        ExtratoContaCorrente = 40,
        [Description("41 - TED Dif. Titularidade")]
        TEDOutraTitularidade = 41,
        [Description("43 - TED - Mesma Titularidade")]
        TEDMesmaTitularidade = 43,
        [Description("44 - TED - Conta Investimento")]
        TEDContaInvestimento = 44,
        [Description("45 - PIX Transferência")]
        PIXTransferencia = 45,
        [Description("47 - PIX QR Code")]
        PIXQrCode = 47,
        [Description("50 - Débito Conta Corrente")]
        DebitoContaCorrente = 50
    }
}
using System.ComponentModel;

namespace BancoBr.Common.Enums
{
    public enum FormaLancamentoEnum
    {
        [Description("Crédito em Conta Corrente - Mesmo Banco")]
        CreditoContaMesmoBanco = 1,
        [Description("Cheque de Pagamento")]
        ChequePagamento = 2,
        [Description("DOC / TED")]
        DOC_TED = 3,
        [Description("Cartão Salário")]
        CartaoSalario = 4, //Somente para tipo de Serviço = 30
        [Description("Crédito Conta Poupança - Mesmo Banco")]
        CreditoContaPoupancaMesmoBanco = 5,
        [Description("O.P. Disposição")]
        OPDisposicao = 10,
        [Description("Trib. Código de Barras")]
        PagamentoTributosCodigoBarra = 11,
        [Description("DARF Normal")]
        TributoDARFNormal = 16,
        [Description("GPS")]
        TributoGPS = 17,
        [Description("DARF Simples")]
        TributoDARFSimples = 18,
        [Description("IPTU")]
        TributoIPTU = 19,
        [Description("Pagamento Autenticado")]
        PagamentoAutenticado = 20,
        [Description("DARJ")]
        TributoDARJ = 21,
        [Description("GARE SP - ICMS")]
        TributoGARE_SP_ICMS = 22,
        [Description("GARE SP - DR")]
        TributoGARE_SP_DR = 23,
        [Description("GARE SP - ITCMD")]
        TributoGARE_SP_ITCMD = 24,
        [Description("IPVA")]
        TributoIPVA = 25,
        [Description("Licenciamento de Veículos")]
        TributoLicenciamento = 26,
        [Description("DPVAT")]
        TributoDPVAT = 27,
        [Description("Liquidação Título - Mesmo Banco")]
        LiquidacaoProprioBanco = 30,
        [Description("Pagamento Título - Outro Banco")]
        PagamentoTituloOutroBanco = 31,
        [Description("Extrato Conta Corrente")]
        ExtratoContaCorrente = 40,
        [Description("TED Dif. Titularidade")]
        TEDOutraTitularidade = 41,
        [Description("TED - Mesma Titularidade")]
        TEDMesmaTitularidade = 43,
        [Description("TED - Conta Investimento")]
        TEDContaInvestimento = 44,
        [Description("PIX Transferência")]
        PIXTransferencia = 45,
        [Description("PIX QR Code")]
        PIXQrCode = 47,
        [Description("Débito Conta Corrente")]
        DebitoContaCorrente = 50
    }
}
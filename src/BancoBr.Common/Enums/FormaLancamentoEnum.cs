namespace BancoBr.Common.Enums
{
    public enum FormaLancamentoEnum
    {
        CreditoContaMesmoBanco = 1,
        ChequePagamento = 2,
        DOC_TED = 3,
        CartaoSalario = 4, //Somente para tipo de Serviço = 30
        CreditoContaPoupancaMesmoBanco = 5,
        OPDisposicao = 10,
        PagamentoTributosCodigoBarra = 11,
        TributoDARFNormal = 16,
        TributoGPS = 17,
        TributoDARFSimples = 18,
        TributoIPTU = 19,
        PagamentoAutenticado = 20,
        TributoDARJ = 21,
        TributoGARE_SP_ICMS = 22,
        TributoGARE_SP_DR = 23,
        TributoGARE_SP_ITCMD = 24,
        TributoIPVA = 25,
        TributoLicenciamento = 26,
        TributoDPVAT = 27,
        LiquidacaoProprioBanco = 30,
        PagamentoTituloOutroBanco = 31,
        ExtratoContaCorrente = 40,
        TEDOutraTitularidade = 41,
        TEDMesmaTitularidade = 43,
        TEDContaInvestimento = 44,
        PIXTransferencia = 45,
        PIXQrCode = 47,
        DebitoContaCorrente = 50
    }
}
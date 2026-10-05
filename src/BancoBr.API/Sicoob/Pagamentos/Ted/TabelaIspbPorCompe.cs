using System.Collections.Generic;

namespace BancoBr.API.Sicoob.Pagamentos.Ted
{
    /// <summary>
    /// ATENÇÃO — tabela de conversão código do banco (COMPE/Febraban) → ISPB, exigida pelo
    /// campo obrigatório "creditorAccount.ispb" da API SPB Transferências do Sicoob.
    ///
    /// COMPE e ISPB são numerações diferentes mantidas pelo Banco Central — não existe
    /// cálculo entre uma e outra, só uma tabela de referência. A lista abaixo cobre apenas as
    /// instituições mais comuns e foi montada a partir de conhecimento público (não consultado
    /// em tempo real), SEM garantia de exatidão ou de estar atualizada.
    ///
    /// Antes de usar em produção: validar cada entrada contra a lista oficial "Participantes
    /// do SPB" do Banco Central (https://www.bcb.gov.br) e completar com os bancos que
    /// faltarem. Um ISPB errado envia a TED para a instituição errada — por isso
    /// <see cref="TedClient"/> lança exceção em vez de enviar com ISPB nulo/adivinhado quando
    /// o código do banco não está nesta tabela.
    /// </summary>
    internal static class TabelaIspbPorCompe
    {
        private static readonly Dictionary<int, string> Ispb = new Dictionary<int, string>
        {
            { 1, "00000000" },      // Banco do Brasil
            { 3, "04902979" },      // Banco da Amazônia
            { 4, "07237373" },      // Banco do Nordeste
            { 21, "28127603" },     // Banestes
            { 25, "60346564" },     // Banco Alfa
            { 33, "90400888" },     // Santander
            { 37, "04913711" },     // Banpará
            { 41, "92702067" },     // Banrisul
            { 47, "13009717" },     // Banese
            { 70, "00000208" },     // BRB - Banco de Brasília
            { 77, "00416968" },     // Banco Inter
            { 102, "02332886" },    // XP Investimentos (Corretora)
            { 104, "00360305" },    // Caixa Econômica Federal
            { 121, "40434681" },    // Banco Agibank
            { 197, "16501555" },    // Stone Pagamentos
            { 208, "30306294" },    // BTG Pactual
            { 212, "92894922" },    // Banco Original
            { 213, "54403563" },    // Banco Arbi
            { 218, "33132044" },    // Banco BS2
            { 237, "60746948" },    // Bradesco
            { 246, "28195667" },    // Banco ABC Brasil
            { 260, "18236120" },    // Nu Pagamentos (Nubank)
            { 290, "22896431" },    // PagSeguro Internet (PagBank)
            { 318, "61186680" },    // Banco BMG
            { 323, "10573521" },    // Mercado Pago
            { 336, "31872495" },    // Banco C6
            { 341, "60701190" },    // Itaú Unibanco
            { 380, "22896431" },    // PicPay
            { 399, "01701201" },    // Kirton Bank (ex-HSBC)
            { 422, "58160789" },    // Banco Safra
            { 623, "59285411" },    // Banco Pan
            { 633, "68900810" },    // Banco Rendimento
            { 655, "59588111" },    // Banco Votorantim (BV)
            { 707, "62232889" },    // Banco Daycoval
            { 745, "33042953" },    // Citibank
            { 748, "01181521" },    // Sicredi
            { 756, "54037916" },    // Sicoob (Bancoob)
        };

        public static string ObterIspb(int codigoCompe)
        {
            if (Ispb.TryGetValue(codigoCompe, out var ispb))
                return ispb;

            throw new System.InvalidOperationException(
                $"ISPB não cadastrado para o banco de código {codigoCompe}. " +
                "Adicione o par código-ISPB em TabelaIspbPorCompe (validado contra a lista " +
                "oficial de Participantes do SPB do Banco Central) antes de enviar esta TED.");
        }
    }
}

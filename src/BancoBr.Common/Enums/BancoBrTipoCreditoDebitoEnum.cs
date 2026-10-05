namespace BancoBr.Common.Enums
{
    /// <summary>
    /// Tipo de lançamento (crédito/débito) de uma transação de extrato, agnóstico de banco.
    /// Normaliza os vários vocabulários textuais usados pelas APIs bancárias (ex.: "C"/"D",
    /// "CREDITO"/"DEBITO", com ou sem acento) em um único conjunto de valores, para que o
    /// consumidor (ERP) não precise conhecer o formato exato enviado por cada banco.
    /// </summary>
    public enum BancoBrTipoCreditoDebitoEnum
    {
        Credito = 0,
        Debito = 1,
    }
}
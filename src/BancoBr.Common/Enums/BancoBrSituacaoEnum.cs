namespace BancoBr.Common.Enums
{
    /// <summary>
    /// Situação de um pagamento do ponto de vista do BancoBr, agnóstica de banco. Normaliza os
    /// vários vocabulários textuais de status usados pelas APIs bancárias (Pix, Boleto,
    /// Convênio) em um único conjunto de estados que o consumidor (ERP) consegue tratar.
    /// </summary>
    public enum BancoBrSituacaoEnum
    {
        NaoIntegrado = 0,
        Agendado = 1,
        Efetivado = 2,
        Cancelado = 3,
        Rejeitado = 4,
    }
}
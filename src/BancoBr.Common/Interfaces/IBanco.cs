namespace BancoBr.Common.Interfaces
{
    public interface IBanco
    {
        /// <summary>
        /// Código do Banco
        /// </summary>
        int Codigo { get; set; }

        /// <summary>
        /// Nome do Banco
        /// </summary>
        string Nome { get; set; }
    }
}

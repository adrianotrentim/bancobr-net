namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Informações sintéticas das arrecadações realizadas numa data de movimento, independente
    /// do banco.
    /// </summary>
    internal class ConciliacaoItem
    {
        public string Situacao { get; set; }

        public string Convenio { get; set; }

        public string SiglaConvenio { get; set; }

        public decimal ValorTotal { get; set; }

        public int Quantidade { get; set; }
    }
}

namespace BancoBr.API.Base.Models
{
    /// <summary>Convênio habilitado para arrecadação, independente do banco.</summary>
    internal class ConvenioHabilitado
    {
        public string Identificador { get; set; }

        public string Sigla { get; set; }

        public string CodigoFebraban { get; set; }

        public int Segmento { get; set; }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Base
{
    public class TrailerLote : Registro
    {
        private readonly List<Registro> _registros;

        public TrailerLote(Lote lote, List<Registro> registros)
            : base(lote.Header.Banco)
        {
            LoteServico = lote.Header.LoteServico;
            TipoRegistro = 5;

            _registros = registros;
        }

        [CNAB(4, 9)]
        public int CNAB1 { get; set; }

        [CNAB(5, 6)]
        public int QuantidadeRegistros => _registros.Count + 2; //2 = Header de Lote + Trailer de Lote
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Base
{
    public class TrailerLoteBase : RegistroBase
    {
        private readonly List<RegistroBase> _registros;

        public TrailerLoteBase(Lote lote, List<RegistroBase> registros)
            : base(lote.Header.Banco)
        {
            LoteServico = lote.Header.LoteServico;
            TipoRegistro = 5;

            _registros = registros;
        }

        [CampoCNAB(4, 9)]
        public int CNAB1 { get; set; }

        [CampoCNAB(5, 6)]
        public int QuantidadeRegistros => _registros.Count + 2; //2 = Header de Lote + Trailer de Lote
    }
}
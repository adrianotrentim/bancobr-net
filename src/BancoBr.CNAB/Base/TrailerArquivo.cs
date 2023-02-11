using System.Collections.Generic;
using System.Linq;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Base
{
    public class TrailerArquivo : Registro
    {
        private readonly List<Lote> _lotes;

        public TrailerArquivo(ArquivoCNAB cnab, List<Lote> lotes)
            : base(cnab.Header.Banco)
        {
            LoteServico = 9999;
            TipoRegistro = 9;

            _lotes = lotes;
        }

        [CampoCNAB(4, 9)]
        public int CNAB1 { get; set; }

        [CampoCNAB(5, 6)]
        public int QuantidadeLotes => _lotes.Count;

        [CampoCNAB(6, 6)]
        public int QuantidadeRegistros => _lotes.Sum(l => l.Trailer.QuantidadeRegistros) + 2; //2 = Header de Arquivo + Trailer de Arquivo

        [CampoCNAB(7, 6)]
        public int QuantidadeContasConciliacao => 0;

        [CampoCNAB(8, 205)]
        public int CNAB2 { get; set; }
    }
}
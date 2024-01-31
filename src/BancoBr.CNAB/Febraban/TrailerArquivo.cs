using System.Collections.Generic;
using System.Linq;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;

namespace BancoBr.CNAB.Febraban
{
    public class TrailerArquivo : RegistroBase
    {
        private readonly List<Lote> _lotes;

        public TrailerArquivo(ArquivoCNAB cnab, List<Lote> lotes)
            : base(cnab.Header.Banco)
        {
            LoteServico = 9999;
            TipoRegistro = 9;

            _lotes = lotes;
        }

        [CampoCNAB(9, 9)]
        public virtual int CNAB1 { get; set; }

        [CampoCNAB(18, 6)]
        public virtual int QuantidadeLotes => _lotes.Count;

        [CampoCNAB(24, 6)]
        public virtual int QuantidadeRegistros => _lotes.Sum(l => l.Trailer.QuantidadeRegistros) + 
                                                  2; //+ 2, porque inclui Header de Arquivo e Trailer de Arquivo

        [CampoCNAB(30, 6)]
        public virtual int QuantidadeContasConciliacao => 0;

        [CampoCNAB(36, 205)]
        public virtual string CNAB2 { get; set; }
    }
}
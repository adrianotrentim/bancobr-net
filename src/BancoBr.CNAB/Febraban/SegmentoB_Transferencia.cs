using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;
using System;
using Banco = BancoBr.Common.Instances.Banco;

namespace BancoBr.CNAB.Febraban
{
    public class SegmentoB_Transferencia : SegmentoB
    {
        public SegmentoB_Transferencia(Banco banco)
            : base(banco)
        {
        }

        [CampoCNAB(15, 3)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(33, 30)]
        public virtual string EnderecoFavorecido { get; set; }

        [CampoCNAB(63, 5)]
        public virtual string NumeroEnderecoFavorecido { get; set; }

        [CampoCNAB(68, 15)]
        public virtual string ComplementoEnderecoFavorecido { get; set; }

        [CampoCNAB(83, 15)]
        public virtual string BairroFavorecido { get; set; }

        [CampoCNAB(98, 20)]
        public virtual string CidadeFavorecido { get; set; }

        [CampoCNAB(118, 8)]
        public virtual int CEPFavorecido { get; set; }

        [CampoCNAB(126, 2)]
        public virtual string UFFavorecido { get; set; }

        [CampoCNAB(128, 8)]
        public DateTime Vencimento { get; set; }

        [CampoCNAB(136, 15)]
        public decimal ValorDocumento { get; set; }

        [CampoCNAB(151, 15)]
        public decimal Abatimento { get; set; }

        [CampoCNAB(166, 15)]
        public decimal Desconto { get; set; }

        [CampoCNAB(181, 15)]
        public decimal Mora { get; set; }

        [CampoCNAB(196, 15)]
        public decimal Multa { get; set; }

        [CampoCNAB(211, 15)]
        public string DocumentoFavorecido { get; set; }

        [CampoCNAB(226, 1)]
        public int AvisoFavorecido { get; set; }
    }
}
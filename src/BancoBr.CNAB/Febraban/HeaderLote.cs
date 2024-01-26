using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using Banco = BancoBr.Common.Instances.Banco;

namespace BancoBr.CNAB.Febraban
{
    public class HeaderLote : HeaderLoteBase
    {
        protected HeaderLote(Banco banco) 
            : base(banco)
        {
        }

        [CampoCNAB(12, 2)]
        public virtual TipoLancamentoEnum TipoLancamento { get; set; }

        [CampoCNAB(14, 3)]
        public virtual int VersaoLote { get; set; }

        [CampoCNAB(17, 1)]
        public virtual string CNAB1 { get; set; }

        [CampoCNAB(18, 1)]
        public virtual TipoInscricaoCPFCNPJEnum TipoInscricaoEmpresa { get; set; }

        [CampoCNAB(19, 14)]
        public virtual long InscricaoEmpresa { get; set; }

        [CampoCNAB(33, 20)]
        public virtual string Convenio { get; set; }

        [CampoCNAB(53, 5)]
        public virtual int NumeroAgencia { get; set; }

        [CampoCNAB(58, 1)]
        public virtual string DVAgencia { get; set; }

        [CampoCNAB(59, 12)]
        public virtual int NumeroConta { get; set; }

        [CampoCNAB(71, 1)]
        public virtual string DVConta { get; set; }

        [CampoCNAB(72, 1)]
        public virtual string DVAgenciaConta { get; set; }

        [CampoCNAB(73, 30)]
        public virtual string NomeEmpresa { get; set; }

        [CampoCNAB(103, 40)]
        public virtual string Informacao1 { get; set; }

        [CampoCNAB(143, 30)]
        public virtual string EnderecoEmpresa { get; set; }

        [CampoCNAB(173, 5)]
        public virtual string NumeroEnderecoEmpresa { get; set; }

        [CampoCNAB(178, 15)]
        public virtual string ComplementoEnderecoEmpresa { get; set; }

        [CampoCNAB(193, 20)]
        public virtual string CidadeEmpresa { get; set; }

        [CampoCNAB(213, 8)]
        public virtual int CEPEmpresa { get; set; }

        [CampoCNAB(221, 2)]
        public virtual string UFEmpresa { get; set; }

        [CampoCNAB(223, 8)]
        public virtual string CNAB2 { get; set; }

        [CampoCNAB(231, 10)]
        public virtual string Ocorrencias { get; set; }
    }
}

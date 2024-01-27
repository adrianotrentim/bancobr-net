using System;
using System.Collections;
using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

namespace BancoBr.CNAB.Bradesco
{
    public sealed class Banco : Base.Banco
    {
        public Banco()
            : base(237, "BRADESCO", 89)
        {
        }

        public override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Common.Instances.Movimento movimento)
        {
            if (segmento is SegmentoA_Transferencia transferencia)
            {
                transferencia.CodigoFinalidadeComplementar = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta == TipoContaEnum.ContaCorrente ? "CC" : "PP";
            }

            return segmento;
        }

        public override RegistroDetalheBase PreencheSegmentoC(RegistroDetalheBase segmento, Common.Instances.Movimento movimento) => null;
    }
}

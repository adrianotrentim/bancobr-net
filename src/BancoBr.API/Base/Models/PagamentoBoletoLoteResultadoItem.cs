using System;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Resultado de um item processado dentro de um lote. Um item com falha não aborta o
    /// restante do lote — fica registrado aqui para o ERP decidir o que fazer.
    /// </summary>
    internal class PagamentoBoletoLoteResultadoItem
    {
        /// <summary>
        /// O mesmo <see cref="Common.Instances.Movimento"/> enviado no lote, já com o resultado
        /// do banco aplicado quando o item foi processado com sucesso.
        /// </summary>
        public Movimento Movimento { get; }

        /// <summary>
        /// Identificador do lançamento no ERP, usado para compor a idempotency key do item.
        /// </summary>
        public Guid IdLancamento { get; }

        public Exception Erro { get; }

        public bool Sucesso => Erro == null;

        private PagamentoBoletoLoteResultadoItem(Movimento movimento, Guid idLancamento, Exception erro)
        {
            Movimento = movimento;
            IdLancamento = idLancamento;
            Erro = erro;
        }

        public static PagamentoBoletoLoteResultadoItem ComSucesso(Movimento movimento, Guid idLancamento) =>
            new PagamentoBoletoLoteResultadoItem(movimento, idLancamento, erro: null);

        public static PagamentoBoletoLoteResultadoItem ComFalha(Movimento movimento, Guid idLancamento, Exception erro) =>
            new PagamentoBoletoLoteResultadoItem(movimento, idLancamento, erro);
    }
}

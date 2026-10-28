using System;
using System.Collections.Generic;
using System.Linq;
using BancoBr.API.Operacoes;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Core.Operacoes
{
    /// <summary>Normalização adicional sem modificar as situações históricas devolvidas em Movimento.</summary>
    internal static class NormalizadorResultado
    {
        internal static ResultadoPagamento Criar(Movimento movimento, ReferenciaPagamento referencia, IReadOnlyList<RetornoBancario> retornos)
        {
            var ultimo = retornos.LastOrDefault();
            JToken raiz = null;
            string original = null;
            foreach (var retorno in retornos.Reverse())
            {
                JToken candidato;
                try { candidato = string.IsNullOrWhiteSpace(retorno.Corpo) ? null : JToken.Parse(retorno.Corpo); }
                catch (JsonException) { continue; }
                if (raiz == null) raiz = candidato;
                var dados = candidato is JObject objeto ? objeto["resultado"] ?? candidato : candidato;
                if (dados is JArray lista) dados = lista.First;
                if (dados is JObject arrecadacao) dados = arrecadacao["arrecadacao"] ?? arrecadacao["pagamento"] ?? dados;
                original = Texto(dados, "status") ?? Texto(dados, "estadoPagamento") ?? Texto(dados, "estado")
                    ?? Texto(dados, "situacaoPagamento") ?? Texto(dados, "situacao");
                if (original != null) break;
            }
            if (original == null) original = MetadadosOperacao.De(movimento).SituacaoOriginal;
            var situacao = SituacaoPagamento.Desconhecida;
            switch (movimento.SituacaoBancoBr)
            {
                case BancoBrSituacaoEnum.Agendado: situacao = SituacaoPagamento.Agendado; break;
                case BancoBrSituacaoEnum.Efetivado: situacao = SituacaoPagamento.Efetivado; break;
                case BancoBrSituacaoEnum.Cancelado: situacao = SituacaoPagamento.Cancelado; break;
                case BancoBrSituacaoEnum.Rejeitado: situacao = SituacaoPagamento.Rejeitado; break;
            }
            var autorizacao = SituacaoAutorizacao.NaoInformada;
            switch (original?.Trim().ToUpperInvariant())
            {
                case "RECEBIDO": case "EM_PROCESSAMENTO": case "PROCESSANDO": case "PENDING_VALIDATION": case "CONSISTENTE": case "READY_TO_PAY": case "PENDING_CONFIRMATION":
                    situacao = SituacaoPagamento.EmProcessamento; break;
                case "AGUARDANDO_APROVACAO": case "AGUARDANDO_AUTORIZACAO": case "PENDENTE_ASSINATURA":
                    situacao = SituacaoPagamento.EmProcessamento; autorizacao = SituacaoAutorizacao.AguardandoAutorizacao; break;
                case "APROVADO":
                    situacao = SituacaoPagamento.EmProcessamento; autorizacao = SituacaoAutorizacao.Autorizado; break;
                case "NAO_APROVADO":
                    situacao = SituacaoPagamento.Rejeitado; autorizacao = SituacaoAutorizacao.Recusado; break;
                case "EXPIRADO":
                    situacao = SituacaoPagamento.Rejeitado; autorizacao = SituacaoAutorizacao.Expirado; break;
                case "AGENDADO": case "SCHEDULED": situacao = SituacaoPagamento.Agendado; break;
                case "SUCESSO": case "PAGO": case "PAYED": case "FINALIZADO": case "FINALIZADO_SUCESSO":
                    situacao = SituacaoPagamento.Efetivado; break;
                case "REFUNDED": case "DEVOLVIDO": situacao = SituacaoPagamento.Devolvido; break;
            }
            // HTTP 202 da arrecadação Sicoob informa pendência de assinatura (mesma evidência usada pelo cliente antigo).
            if (referencia.Banco == (int)BancoEnum.Sicoob && (referencia.Modalidade == ModalidadePagamento.Convenio || referencia.Modalidade == ModalidadePagamento.Boleto) && ultimo?.HttpStatusCode == 202)
            {
                autorizacao = SituacaoAutorizacao.AguardandoAutorizacao;
                situacao = SituacaoPagamento.EmProcessamento;
            }
            // Pagamentos em Lote BB distingue o estado da requisição do estado de liquidação do lançamento.
            if (referencia.Banco == (int)BancoEnum.BancoDoBrasil && raiz is JObject bb)
            {
                var estado = (int?)(bb["estadoRequisicao"] ?? bb["codigoEstadoRequisicao"] ?? bb["codigoEstado"]);
                if (estado.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(original)) original = "REQUISICAO:" + estado.Value;
                    if (Texto(bb, "estadoPagamento") == null && movimento.SituacaoBancoBr != BancoBrSituacaoEnum.Rejeitado) situacao = SituacaoPagamento.EmProcessamento;
                    if (estado == 4) autorizacao = SituacaoAutorizacao.AguardandoAutorizacao;
                    if (estado == 9 || estado == 10) autorizacao = SituacaoAutorizacao.Autorizado;
                    if (estado == 3 || estado == 7) situacao = SituacaoPagamento.Rejeitado;
                }
            }
            return new ResultadoPagamento
            {
                IdOperacao = referencia.IdOperacao, Referencia = referencia, Movimento = movimento,
                Situacao = situacao, Autorizacao = autorizacao, SituacaoOriginal = original,
                Motivo = movimento.DetalheRejeicaoBancoBr, RetornosBanco = retornos.ToArray()
            };
        }

        private static string Texto(JToken dados, string nome)
        {
            if (!(dados is JObject objeto)) return null;
            var campo = objeto[nome];
            if (campo is JObject situacao) return (string)situacao["descricao"];
            return campo is JValue ? (string)campo : null;
        }
    }
}

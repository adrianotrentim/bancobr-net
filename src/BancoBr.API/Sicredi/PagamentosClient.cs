using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Sicredi
{
    /// <summary>Protocolo de pagamentos Multipag. A criação não equivale à liquidação.</summary>
    internal sealed class PagamentosClient
    {
        internal readonly SicrediHttpGateway Gateway;
        internal PagamentosClient(SicrediHttpGateway gateway) { Gateway = gateway; }
        internal static string Recurso(Movimento m)
        {
            if (m.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra) return "boletos";
            if (m.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra) return "tributos/barras";
            if (m.MovimentoItem is MovimentoItemTransferenciaPIX) return "pix";
            throw new NotSupportedException("Multipag suporta boleto, tributo com barras e Pix por chave.");
        }

        internal async Task<Movimento> PagarAsync(Movimento m, string chave, CancellationToken ct)
        {
            if (m.DataPagamento == default || m.ValorPagamento < 0) throw new ArgumentException("Informe data e valor do pagamento.");
            var body = Gateway.Conta();
            body["dataPagamento"] = m.DataPagamento.ToString("yyyy-MM-dd");
            body["valorPagamento"] = m.ValorPagamento;
            var identificador = m.NumeroDocumento;
            if (string.IsNullOrWhiteSpace(identificador) || identificador.Length > 100)
                throw new ArgumentException("Informe NumeroDocumento como identificador do associado (até 100 caracteres).");
            body["identificadorPagamentoAssociado"] = identificador;
            if (string.IsNullOrWhiteSpace(m.IdRequisicaoBancoBr)) m.IdRequisicaoBancoBr = chave ?? Guid.NewGuid().ToString();
            if (m.IdRequisicaoBancoBr.Length > 100) throw new ArgumentException("idTransacao excede 100 caracteres.");
            body["idTransacao"] = m.IdRequisicaoBancoBr;
            var recurso = Recurso(m);
            string barras = null;
            if (m.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra boleto)
            {
                barras = boleto.CodigoBarras;
                if (!string.IsNullOrWhiteSpace(m.Favorecido?.CPF_CNPJ)) body["cpfCnpjBeneficiario"] = SicrediHttpGateway.Digitos(m.Favorecido.CPF_CNPJ);
            }
            else if (m.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra tributo)
            {
                barras = tributo.CodigoBarra;
                if (!string.IsNullOrEmpty(tributo.IdentificadorFgts)) body["idFgts"] = tributo.IdentificadorFgts;
            }
            else if (m.MovimentoItem is MovimentoItemTransferenciaPIX pix)
            {
                if (string.IsNullOrWhiteSpace(pix.ChavePIX) || string.IsNullOrWhiteSpace(m.Favorecido?.CPF_CNPJ))
                    throw new ArgumentException("Informe a chave Pix e o documento do favorecido.");
                body["chavePix"] = pix.ChavePIX.Contains("@") ? pix.ChavePIX.ToLowerInvariant() : pix.ChavePIX;
                body["documentoBeneficiario"] = SicrediHttpGateway.Digitos(m.Favorecido.CPF_CNPJ);
                if (!string.IsNullOrWhiteSpace(m.Favorecido.Nome)) body["nomeBeneficiario"] = m.Favorecido.Nome;
            }
            if (barras != null)
            {
                barras = SicrediHttpGateway.Digitos(barras);
                if (barras.Length != 44) throw new ArgumentException("Sicredi exige código de barras de 44 dígitos.");
                body["codigoBarra"] = barras;
            }
            else if (recurso != "pix") throw new ArgumentException("Informe o código de barras.");
            var resposta = await Gateway.JsonAsync(HttpMethod.Post, Gateway.Pagamentos + recurso + (recurso == "pix" ? "/chave" : ""), body, ct).ConfigureAwait(false);
            m.NumeroDocumentoNoBanco = m.IdRequisicaoBancoBr;
            Aplicar(m, resposta);
            return m;
        }

        private static string Id(Movimento m) => !string.IsNullOrWhiteSpace(m.NumeroDocumentoNoBanco)
            ? Uri.EscapeDataString(m.NumeroDocumentoNoBanco) : throw new ArgumentException("Informe o idTransacao em NumeroDocumentoNoBanco.");

        internal async Task<Movimento> ConsultarAsync(Movimento m, CancellationToken ct)
        {
            Aplicar(m, await Gateway.JsonAsync(HttpMethod.Get, Gateway.Pagamentos + Recurso(m) + "/" + Id(m), null, ct).ConfigureAwait(false));
            return m;
        }

        internal async Task<Movimento> CancelarAsync(Movimento m, CancellationToken ct)
        {
            Id(m);
            var body = Gateway.Conta();
            body["idTransacao"] = m.NumeroDocumentoNoBanco;
            await Gateway.JsonAsync(new HttpMethod("PATCH"), Gateway.Pagamentos + Recurso(m) + "/cancelamentos", body, ct).ConfigureAwait(false);
            m.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado;
            m.SituacaoOriginalBancoBr = "CANCELADO";
            return m;
        }

        internal Task<byte[]> ComprovanteAsync(Movimento m, CancellationToken ct) => Gateway.SendAsync(HttpMethod.Get,
            Gateway.Pagamentos + Recurso(m) + "/" + Id(m) + "/comprovantes", null, ct, true);

        private static void Aplicar(Movimento m, JObject r)
        {
            var status = (string)r["status"];
            m.SituacaoOriginalBancoBr = status;
            switch (status)
            {
                case "SUCESSO": m.SituacaoBancoBr = BancoBrSituacaoEnum.Efetivado; break;
                case "CANCELADO": m.SituacaoBancoBr = BancoBrSituacaoEnum.Cancelado; break;
                case "ERRO": case "NAO_APROVADO": case "EXPIRADO": m.SituacaoBancoBr = BancoBrSituacaoEnum.Rejeitado; break;
                case "RECEBIDO": case "AGENDADO": case "AGUARDANDO_APROVACAO": case "APROVADO": m.SituacaoBancoBr = BancoBrSituacaoEnum.Agendado; break;
                default: m.SituacaoBancoBr = BancoBrSituacaoEnum.NaoIntegrado; break;
            }
            m.DetalheRejeicaoBancoBr = m.SituacaoBancoBr == BancoBrSituacaoEnum.Rejeitado ? status : null;
        }
    }
}

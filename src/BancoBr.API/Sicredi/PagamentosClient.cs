using System;
using BancoBr.API.Core.Operacoes;
using System.Globalization;
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
            body["dataPagamento"] = m.DataPagamento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            body["valorPagamento"] = m.ValorPagamento;
            var identificador = m.NumeroDocumento;
            if (string.IsNullOrWhiteSpace(identificador) || identificador.Length > 100)
                throw new ArgumentException("Informe NumeroDocumento como identificador do associado (até 100 caracteres).");
            body["identificadorPagamentoAssociado"] = identificador;
            if (string.IsNullOrWhiteSpace(MetadadosOperacao.De(m).IdRequisicao)) MetadadosOperacao.De(m).IdRequisicao = chave ?? Guid.NewGuid().ToString();
            if (MetadadosOperacao.De(m).IdRequisicao.Length > 100) throw new ArgumentException("idTransacao excede 100 caracteres.");
            body["idTransacao"] = MetadadosOperacao.De(m).IdRequisicao;
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
            m.NumeroDocumentoNoBanco = MetadadosOperacao.De(m).IdRequisicao;
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
            MetadadosOperacao.De(m).SituacaoOriginal = "CANCELADO";
            return m;
        }

        internal Task<byte[]> ComprovanteAsync(Movimento m, CancellationToken ct) => Gateway.SendAsync(HttpMethod.Get,
            Gateway.Pagamentos + Recurso(m) + "/" + Id(m) + "/comprovantes", null, ct, true);

        private static void Aplicar(Movimento m, JObject r)
        {
            // Respostas parciais de situação preservam os dados já conhecidos do lançamento.
            var valor = r.Value<decimal?>("valorPagamento");
            if (valor.HasValue) m.ValorPagamento = valor.Value;
            var data = r.Value<DateTime?>("dataPagamento");
            if (data.HasValue) m.DataPagamento = data.Value;
            var documento = r.Value<string>("identificadorPagamentoAssociado");
            if (documento != null) m.NumeroDocumento = documento;
            var idTransacao = r.Value<string>("idTransacao");
            if (!string.IsNullOrWhiteSpace(idTransacao))
            {
                m.NumeroDocumentoNoBanco = idTransacao;
                MetadadosOperacao.De(m).IdRequisicao = idTransacao;
            }

            var codigoBarra = r.Value<string>("codigoBarra");
            if (m.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra boleto && codigoBarra != null)
                boleto.CodigoBarras = codigoBarra;
            if (m.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra tributo)
            {
                if (codigoBarra != null) tributo.CodigoBarra = codigoBarra;
                var idFgts = r.Value<string>("idFgts");
                if (idFgts != null) tributo.IdentificadorFgts = idFgts;
            }
            if (m.MovimentoItem is MovimentoItemTransferenciaPIX pix)
            {
                var chavePix = r.Value<string>("chavePix");
                if (chavePix != null) pix.ChavePIX = chavePix;
                var nome = r.Value<string>("nomeBeneficiario");
                var cpfCnpj = r.Value<string>("documentoBeneficiario");
                if (nome != null || cpfCnpj != null)
                {
                    if (m.Favorecido == null) m.Favorecido = new Favorecido();
                    if (nome != null)
                    {
                        m.Favorecido.Nome = nome;
                        pix.TitularConfirmadoNome = nome;
                    }
                    if (cpfCnpj != null)
                    {
                        m.Favorecido.CPF_CNPJ = cpfCnpj;
                        pix.TitularConfirmadoDocumento = cpfCnpj;
                    }
                }
            }

            var status = (string)r["status"];
            MetadadosOperacao.De(m).SituacaoOriginal = status;
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

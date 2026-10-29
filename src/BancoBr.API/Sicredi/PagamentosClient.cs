using System;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Linq;
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
        private static readonly ConditionalWeakTable<Movimento, StrongBox<string>> Identidades = new ConditionalWeakTable<Movimento, StrongBox<string>>();
        private static StrongBox<string> IdentidadeDe(Movimento movimento) => Identidades.GetValue(movimento, _ => new StrongBox<string>());
        internal readonly SicrediHttpGateway Gateway;
        internal PagamentosClient(SicrediHttpGateway gateway) { Gateway = gateway; }
        internal static string Recurso(Movimento m)
        {
            if (m.MovimentoItem is MovimentoItemPagamentoTituloCodigoBarra) return "boletos";
            if (m.MovimentoItem is MovimentoItemPagamentoConvenioCodigoBarra) return "tributos/barras";
            if (m.MovimentoItem is MovimentoItemTransferenciaPIX) return "pix";
            throw new NotSupportedException("Multipag suporta boleto, tributo com barras e Pix por chave ou dados bancários.");
        }

        internal async Task<Movimento> PagarAsync(Movimento m, string chave, CancellationToken ct)
        {
            // Uma referência persistida identifica um pagamento já enviado, mesmo em outra instância.
            if (!string.IsNullOrWhiteSpace(m.NumeroDocumentoNoBanco))
                return await ConsultarAsync(m, ct).ConfigureAwait(false);

            if (m.DataPagamento == default || m.ValorPagamento < 0) throw new ArgumentException("Informe data e valor do pagamento.");
            var body = Gateway.Conta();
            body["dataPagamento"] = m.DataPagamento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            body["valorPagamento"] = m.ValorPagamento;
            var identificador = m.NumeroDocumento;
            if (string.IsNullOrWhiteSpace(identificador) || identificador.Length > 100)
                throw new ArgumentException("Informe NumeroDocumento como identificador do associado (até 100 caracteres).");
            body["identificadorPagamentoAssociado"] = identificador;
            var identidade = IdentidadeDe(m);
            var idTransacao = string.IsNullOrWhiteSpace(identidade.Value) ? chave ?? Guid.NewGuid().ToString() : identidade.Value;
            if (idTransacao.Length > 100) throw new ArgumentException("idTransacao excede 100 caracteres.");
            identidade.Value = idTransacao;
            body["idTransacao"] = idTransacao;
            var recurso = Recurso(m);
            var rota = recurso;
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
                if (string.IsNullOrWhiteSpace(m.Favorecido?.CPF_CNPJ))
                    throw new ArgumentException("Informe o documento do favorecido.");
                body["documentoBeneficiario"] = SicrediHttpGateway.Digitos(m.Favorecido.CPF_CNPJ);
                if (!string.IsNullOrWhiteSpace(m.Favorecido.Nome)) body["nomeBeneficiario"] = m.Favorecido.Nome;
                if (!string.IsNullOrWhiteSpace(pix.ChavePIX))
                {
                    rota += "/chave";
                    body["chavePix"] = pix.ChavePIX.Contains("@") ? pix.ChavePIX.ToLowerInvariant() : pix.ChavePIX;
                }
                else
                {
                    rota += "/dados-bancarios";
                    PreencherDadosBancarios(body, m, pix);
                }
            }
            if (barras != null)
            {
                barras = SicrediHttpGateway.Digitos(barras);
                if (barras.Length != 44) throw new ArgumentException("Sicredi exige código de barras de 44 dígitos.");
                body["codigoBarra"] = barras;
            }
            else if (recurso != "pix") throw new ArgumentException("Informe o código de barras.");
            var resposta = await Gateway.JsonAsync(HttpMethod.Post, Gateway.Pagamentos + rota, body, ct).ConfigureAwait(false);
            m.NumeroDocumentoNoBanco = IdentidadeDe(m).Value;
            Aplicar(m, resposta);
            return m;
        }

        private static void PreencherDadosBancarios(JObject body, Movimento movimento, MovimentoItemTransferenciaPIX pix)
        {
            if (string.IsNullOrWhiteSpace(movimento.Favorecido.Nome) || movimento.Favorecido.Nome.Length > 100)
                throw new ArgumentException("Informe o nome do favorecido (até 100 caracteres) para Pix por dados bancários.");
            var ispb = pix.Ispb?.Trim();
            if (string.IsNullOrEmpty(ispb) || ispb.Length > 8 || ispb.Any(c => c < '0' || c > '9'))
                throw new ArgumentException("Informe o ISPB numérico do favorecido (até 8 dígitos); o Sicredi exige ISPB, não código COMPE.");
            if (pix.NumeroAgencia < 0 || pix.NumeroAgencia > 9999 || pix.NumeroConta <= 0)
                throw new ArgumentException("Informe a agência (até 4 dígitos) e a conta do favorecido.");
            var conta = pix.NumeroConta.ToString(CultureInfo.InvariantCulture) + pix.DVConta;
            if (conta.Length > 20)
                throw new ArgumentException("A conta do favorecido com DV deve ter até 20 caracteres.");
            string tipoConta;
            switch (pix.TipoConta)
            {
                case TipoContaEnum.ContaCorrente: tipoConta = "CORRENTE"; break;
                case TipoContaEnum.ContaPoupanca: tipoConta = "POUPANCA"; break;
                case TipoContaEnum.ContaPagamento: tipoConta = "PAGAMENTO"; break;
                default: throw new NotSupportedException("Tipo de conta não suportado para Pix Sicredi.");
            }
            body["agenciaBeneficiario"] = pix.NumeroAgencia.ToString("D4", CultureInfo.InvariantCulture);
            body["contaBeneficiario"] = conta;
            body["ispbBeneficiario"] = ispb.PadLeft(8, '0');
            body["tipoContaBeneficiario"] = tipoConta;
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
                IdentidadeDe(m).Value = idTransacao;
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

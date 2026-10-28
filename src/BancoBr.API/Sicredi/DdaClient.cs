using System;
using System.Globalization;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BancoBr.API.Base.Models;
using BancoBr.Common.Instances;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Sicredi
{
    internal sealed class DdaClient
    {
        private readonly SicrediHttpGateway _gateway;
        internal DdaClient(SicrediHttpGateway gateway) { _gateway = gateway; }
        internal async Task<IReadOnlyList<Movimento>> ConsultarAsync(DateTime inicio, DateTime fim, SituacaoBoletoEnum situacao, TipoDataConsultaEnum tipoData, CancellationToken ct)
        {
            if (situacao != SituacaoBoletoEnum.EmAberto || tipoData != TipoDataConsultaEnum.Vencimento) throw new NotSupportedException("DDA Sicredi consulta boletos em aberto por vencimento.");
            if (inicio.Date > fim.Date) throw new ArgumentException("Período invertido.");
            var resultado = new List<Movimento>();
            for (var primeiro = 1; ; primeiro += 100)
            {
                var url = _gateway.Raiz + (_gateway.Homologacao ? "multipag-sandbox" : "multipag") + "/v1/dda/boletos?dataInicio=" + inicio.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "&dataFim=" + fim.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "&cnpjCpfPagador=" + SicrediHttpGateway.Digitos(_gateway.Origem.CPF_CNPJ) + "&itemInicial=" + primeiro + "&itemFinal=" + (primeiro + 99);
                var r = await _gateway.JsonAsync(HttpMethod.Get, url, null, ct).ConfigureAwait(false);
                var boletos = (JArray)r["boletos"] ?? throw new Newtonsoft.Json.JsonSerializationException("DDA sem boletos.");
                foreach (var b in boletos) resultado.Add(new Movimento { MovimentoItem = new MovimentoItemDDA
                {
                    NomeRazaoSocialBeneficiario = (string)b["nomeBeneficiario"], NumeroCpfCnpjBeneficiario = (string)b["cpfCnpjBeneficiario"],
                    NumeroCpfCnpjPagador = (string)b["cpfCnpjPagador"], DataVencimentoBoleto = b.Value<DateTime>("dataVencimento"),
                    ValorBoleto = b.Value<decimal>("valorTitulo"), NumeroCodigoBarras = (string)b["codigoBarras"]
                }});
                if (boletos.Count < 100) break;
            }
            return resultado;
        }
    }
}

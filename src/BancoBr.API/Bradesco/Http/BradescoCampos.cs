using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace BancoBr.API.Bradesco.Http
{
    /// <summary>
    /// Conversões dos formatos do Bradesco, que mudam de uma API para outra: Boleto usa AAAAMMDD
    /// numérico, TED usa DD.MM.AAAA e Pix usa texto com ponto decimal.
    /// </summary>
    internal static class BradescoCampos
    {
        internal static string Digitos(string valor) => new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>AAAAMMDD numérico (Boleto).</summary>
        internal static int Data(DateTime data) => int.Parse(data.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        /// <summary>DD.MM.AAAA (TED).</summary>
        internal static string DataComPontos(DateTime data) => data.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        /// <summary>HHMMSS numérico (Boleto).</summary>
        internal static int Hora(DateTime hora) => int.Parse(hora.ToString("HHmmss", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        internal static string Truncar(string texto, int tamanho)
        {
            texto = (texto ?? string.Empty).Trim();
            return texto.Length <= tamanho ? texto : texto.Substring(0, tamanho);
        }

        /// <summary>"J" para CNPJ (14 dígitos) e "F" para CPF (11).</summary>
        internal static string TipoPessoa(string documento) => Digitos(documento).Length == 14 ? "J" : "F";

        /// <summary>
        /// Separa o documento como o Bradesco pede: CNPJ em raiz (8) + filial (4) + controle (2); CPF em
        /// base (9) + filial 0 + controle (2).
        /// </summary>
        internal static (long Raiz, int Filial, int Controle) DividirDocumento(string documento)
        {
            var d = Digitos(documento);
            if (d.Length == 11)
                return (long.Parse(d.Substring(0, 9), CultureInfo.InvariantCulture), 0, int.Parse(d.Substring(9), CultureInfo.InvariantCulture));
            if (d.Length == 14)
                return (long.Parse(d.Substring(0, 8), CultureInfo.InvariantCulture), int.Parse(d.Substring(8, 4), CultureInfo.InvariantCulture), int.Parse(d.Substring(12), CultureInfo.InvariantCulture));
            throw new ArgumentException("Informe um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.");
        }

        /// <summary>Conta com o dígito verificador numérico concatenado, como a TED exige ("contaComDigito").</summary>
        internal static long ContaComDigito(long conta, string digito)
        {
            digito = (digito ?? string.Empty).Trim();
            if (digito.Length > 0 && digito.Any(c => !char.IsDigit(c)))
                throw new ArgumentException("O Bradesco exige o dígito verificador da conta numérico nesta operação.");
            return long.Parse(conta.ToString(CultureInfo.InvariantCulture) + digito, CultureInfo.InvariantCulture);
        }

        /// <summary>Texto do valor JSON (número ou texto), sem espaços, ou nulo quando ausente/vazio.</summary>
        internal static string Texto(JToken token)
        {
            if (!(token is JValue valor) || valor.Value == null) return null;
            var texto = Convert.ToString(valor.Value, CultureInfo.InvariantCulture)?.Trim();
            return string.IsNullOrEmpty(texto) ? null : texto;
        }

        internal static decimal? Decimal(JToken token) =>
            decimal.TryParse(Texto(token), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) ? valor : (decimal?)null;

        internal static long? Inteiro(JToken token) =>
            long.TryParse(Texto(token), NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) ? valor : (long?)null;

        /// <summary>Valor no formato do extrato ("6.759,00"); sem vírgula, lê o texto como decimal comum. Vazio não é valor.</summary>
        internal static decimal? DecimalBr(JToken token)
        {
            var texto = Texto(token);
            if (texto == null) return null;
            if (texto.Contains(","))
                texto = texto.Replace(".", string.Empty).Replace(',', '.');
            return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor) ? valor : (decimal?)null;
        }

        /// <summary>Aplica o sinal do extrato ("-" negativo; "+" ou vazio positivo) ao valor absoluto.</summary>
        internal static decimal ComSinal(decimal valor, string sinal) => sinal == "-" ? -Math.Abs(valor) : Math.Abs(valor);

        /// <summary>Data DD/MM/AAAA (extrato).</summary>
        internal static DateTime? DataComBarras(JToken token) =>
            DateTime.TryParseExact(Texto(token), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data : (DateTime?)null;

        /// <summary>Data AAAAMMDD (número ou texto); zero ou vazio não é data.</summary>
        internal static DateTime? DataAAAAMMDD(JToken token) =>
            DateTime.TryParseExact(Texto(token), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data : (DateTime?)null;
    }
}

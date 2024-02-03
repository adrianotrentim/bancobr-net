using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BancoBr.Common.Core
{
    public static class StringExtensions
    {
        public static string RemoveAccents(this string value)
        {
            if (value == null)
                return null;

            if (string.IsNullOrEmpty(value))
                return "";

            if (string.IsNullOrWhiteSpace(value))
                return "";

            var retorno = "";

            var comacentos = "ÇÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜçáàâãäéèêëíìîïóòôõöúùûü\"'ºª&~´^#²³°″Ø–—\\/\t";
            var semacento = "CAAAAAEEEEIIIIOOOOOUUUUcaaaaaeeeeiiiiooooouuuu  oaE    23o O---- ";

            for (int i = 0; i < value.Length; i++)
            {
                var pos = comacentos.IndexOf(value[i]);
                retorno += (pos == -1) ? value[i] : semacento[pos];
            }

            return retorno
                .Replace("  ", " ")
                .Replace("§", "ART.")
                .Trim();
        }

        public static string JustNumbers(this string value)
        {
            if (value == null)
                return null;

            try
            {
                var regexObj = new Regex(@"[^\d]");
                return regexObj.Replace(value, "");
            }
            catch
            {
                // Syntax error in the regular expression
                return "";
            }
        }

        public static string Truncate(this string value, int length)
        {
            return value.Length > length ? value.Substring(0, length).Trim() : value.Trim();
        }

        public static bool IsValidEmail(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                // Normalize the domain
                value = Regex.Replace(
                    value,
                    @"(@)(.+)$",
                    match =>
                    {
                        // Use IdnMapping class to convert Unicode domain names.
                        var idn = new System.Globalization.IdnMapping();

                        // Pull out and process domain name (throws ArgumentException on invalid)
                        var domainName = idn.GetAscii(match.Groups[2].Value);

                        return match.Groups[1].Value + domainName;
                    }, RegexOptions.None,
                    TimeSpan.FromMilliseconds(200)
                );

                // Return true if strIn is in valid e-mail format.
                return Regex.IsMatch(value,
                    @"^(?("")(""[^""]+?""@)|((0-9a-z)|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidCPFCNPJ(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var _numbers = value.JustNumbers();

            switch (_numbers.Length)
            {
                case 11:
                    return _numbers.IsValidCPF();
                case 14:
                    return _numbers.IsValidCNPJ();
                default:
                    return false;
            }
        }

        public static bool IsValidCPF(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.JustNumbers();

            if (value.Length != 11)
                return false;

            var equal = true;
            for (var i = 1; i < 11 && equal; i++)
                if (value[i] != value[0])
                    equal = false;

            if (equal || value == "12345678909")
                return false;

            var numbers = new int[11];

            for (var i = 0; i < 11; i++)
                numbers[i] = int.Parse(
                    value[i].ToString());

            var sum = 0;
            for (var i = 0; i < 9; i++)
                sum += (10 - i) * numbers[i];

            var result = sum % 11;

            if (result == 1 || result == 0)
            {
                if (numbers[9] != 0)
                    return false;
            }
            else if (numbers[9] != 11 - result)
                return false;

            sum = 0;
            for (var i = 0; i < 10; i++)
                sum += (11 - i) * numbers[i];

            result = sum % 11;

            if (result == 1 || result == 0)
            {
                if (numbers[10] != 0)
                    return false;
            }
            else
            if (numbers[10] != 11 - result)
                return false;

            return true;
        }

        public static bool IsValidCNPJ(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.JustNumbers();

            if (value.Length != 14)
                return false;

            const string ftmt = "6543298765432";
            var digits = new int[14];
            var sum = new int[2];
            sum[0] = 0;
            sum[1] = 0;

            var result = new int[2];
            result[0] = 0;
            result[1] = 0;

            var CNPJOk = new bool[2];
            CNPJOk[0] = false;
            CNPJOk[1] = false;

            try
            {
                int nrDig;
                for (nrDig = 0; nrDig < 14; nrDig++)
                {
                    digits[nrDig] = int.Parse(
                        value.Substring(nrDig, 1));
                    if (nrDig <= 11)
                        sum[0] += (digits[nrDig] *
                                   int.Parse(ftmt.Substring(
                                       nrDig + 1, 1)));
                    if (nrDig <= 12)
                        sum[1] += (digits[nrDig] *
                                   int.Parse(ftmt.Substring(
                                       nrDig, 1)));
                }

                for (nrDig = 0; nrDig < 2; nrDig++)
                {
                    result[nrDig] = (sum[nrDig] % 11);
                    if ((result[nrDig] == 0) || (
                            result[nrDig] == 1))
                        CNPJOk[nrDig] = (
                            digits[12 + nrDig] == 0);
                    else
                        CNPJOk[nrDig] = (
                            digits[12 + nrDig] == (
                                11 - result[nrDig]));
                }
                return (CNPJOk[0] && CNPJOk[1]);
            }
            catch
            {
                return false;
            }
        }
    }
}

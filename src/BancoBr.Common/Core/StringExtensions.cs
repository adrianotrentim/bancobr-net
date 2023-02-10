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
    }
}

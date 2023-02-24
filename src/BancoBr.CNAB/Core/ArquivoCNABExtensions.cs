using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BancoBr.CNAB.Base;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;

namespace BancoBr.CNAB.Core
{
    public static class ArquivoCNABExtensions
    {
        public static string Exportar(this ArquivoCNAB cnab)
        {
            var arquivo = new StringBuilder();

            arquivo.AppendLine(cnab.Header.ExportarCampos());

            foreach (var lote in cnab.Lotes)
            {
                arquivo.AppendLine(lote.Header.ExportarCampos());

                foreach (var registro in lote.Registros)
                    arquivo.AppendLine(registro.ExportarCampos());

                arquivo.AppendLine(lote.Trailer.ExportarCampos());
            }

            arquivo.Append(cnab.Trailer.ExportarCampos());

            return arquivo.ToString();
        }

        private static string ExportarCampos(this RegistroBase registro)
        {
            var ret = new StringBuilder();

            var listaCampos = registro.GetType()
                .GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(CampoCNABAttribute), true).Any())
                .Where(p => !((CampoCNABAttribute)p.GetCustomAttributes(typeof(CampoCNABAttribute), true)[0]).Desabilitado)
                .OrderBy(p => ((CampoCNABAttribute)p.GetCustomAttributes(typeof(CampoCNABAttribute), true)[0]).Posicao)
                .ToList();

            foreach (var campo in listaCampos)
            {
                var campoCNAB = ((CampoCNABAttribute)campo.GetCustomAttributes(typeof(CampoCNABAttribute), true)[0]);
                var charPrrenchimento = campoCNAB.CharPreenchimento?.ToString();
                var valueString = "";

                if (campo.PropertyType == typeof(string))
                    valueString = (campo.GetValue(registro, null)?.ToString() ?? "").RemoveAccents().ToUpper().Truncate(campoCNAB.Tamanho).PadRight(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? ' ' : charPrrenchimento[0]);
                else if (campo.PropertyType.IsEnum)
                    valueString = ((int)(campo.GetValue(registro, null) ?? 0)).ToString().Truncate(campoCNAB.Tamanho).PadLeft(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? '0' : charPrrenchimento[0]);
                else if (campo.PropertyType == typeof(int) || campo.PropertyType == typeof(int?) || campo.PropertyType == typeof(long) || campo.PropertyType == typeof(long?))
                    valueString = (campo.GetValue(registro, null) ?? 0).ToString().Truncate(campoCNAB.Tamanho).PadLeft(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? '0' : charPrrenchimento[0]);
                else if (campo.PropertyType == typeof(decimal) || campo.PropertyType == typeof(decimal?))
                    valueString = (decimal.Parse(campo.GetValue(registro, null)?.ToString() ?? "0") * 100).ToString("0").Truncate(campoCNAB.Tamanho).PadLeft(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? '0' : charPrrenchimento[0]);
                else if (campo.PropertyType == typeof(DateTime) || campo.PropertyType == typeof(DateTime?))
                {
                    var value = (DateTime?)campo.GetValue(registro, null);

                    if (value == DateTime.MinValue)
                        value = null;

                    var formato = campoCNAB.Formatacao;
                    valueString = (value?.ToString(string.IsNullOrEmpty(formato) ? "ddMMyyyy" : formato) ?? "").Truncate(campoCNAB.Tamanho).PadLeft(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? '0' : charPrrenchimento[0]);
                }

                ret.Append(valueString);
            }

            return ret.ToString();
        }
    }
}

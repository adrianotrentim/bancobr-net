using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Interfaces.CNAB;

namespace BancoBr.CNAB.Core
{
    public static class ArquivoCNABExtensions
    {
        #region ::. Métodos de Exportação .::

        public static string Exportar(this ArquivoCNAB cnab)
        {
            var arquivo = new StringBuilder();

            arquivo.AppendLine(cnab.Header.ExportarCampos());

            foreach (var lote in cnab.Lotes)
            {
                arquivo.AppendLine(lote.Header.ExportarCampos());

                foreach (var registro in lote.Detalhe)
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
                var alinhamentoPreenchimento = campoCNAB.AlinhamentoPreenchimento;
                var valueString = "";

                if (campo.PropertyType == typeof(string))
                    if (alinhamentoPreenchimento == AlinhamentoPreenchimentoEnum.PreencherADireita)
                        valueString = (campo.GetValue(registro, null)?.ToString() ?? "").RemoveAccents().ToUpper().Truncate(campoCNAB.Tamanho).PadRight(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? ' ' : charPrrenchimento[0]);
                    else
                        valueString = (campo.GetValue(registro, null)?.ToString() ?? "").RemoveAccents().ToUpper().Truncate(campoCNAB.Tamanho).PadLeft(campoCNAB.Tamanho, string.IsNullOrEmpty(charPrrenchimento) ? ' ' : charPrrenchimento[0]);
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

        #endregion

        #region ::. Métodos de Importação .::

        public static void Importar(this ArquivoCNAB cnab, IEnumerable<string> linhas)
        {
            Lote lote = null;

            foreach (var linha in linhas)
            {
                if (string.IsNullOrEmpty(linha))
                    continue;

                var tipoRegistro = linha.Substring(7, 1);
                RegistroBase instanciaRegistro = null;

                if (tipoRegistro == "0") //Header de Arquivo
                {
                    instanciaRegistro = cnab.Header;
                }
                else if (tipoRegistro == "1") //Header de Lote
                {
                    var formaPagmanento = Convert.ToInt32(linha.Substring(11, 2));

                    lote = new Lote
                    {
                        Header = cnab.Banco.NovoHeaderLote((TipoLancamentoEnum)formaPagmanento)
                    };

                    cnab.Lotes.Add(lote);

                    instanciaRegistro = lote.Header;
                }
                else if (tipoRegistro == "2") //Detalhe Iniciais de Lote
                {

                }
                else if (tipoRegistro == "3") //Detalhe Detalhe
                {
                    var bancoType = cnab.Banco.GetType();
                    var tipoSegmento = linha.Substring(13, 1);

                    RegistroDetalheBase registro = null;

                    switch (lote.Header.Servico)
                    {
                        case TipoServicoEnum.PagamentoSalarios: //Ir Adicionando os tipos que se assemelham com os pagamentos de salários
                            registro = (RegistroDetalheBase)bancoType.InvokeMember("NovoSegmento" + tipoSegmento, BindingFlags.InvokeMethod, null, cnab.Banco, new object[] { ((HeaderLote_PagamentoTransferencia)lote.Header).TipoLancamento });
                            break;
                    }

                    lote.Detalhe.Add(registro);

                    instanciaRegistro = registro;
                }
                else if (tipoRegistro == "4") //Detalhe Finais de Lote
                {

                }
                else if (tipoRegistro == "5") //Trailer de Lote
                {
                    lote.Trailer = cnab.Banco.NovoTrailerLote(lote);
                    instanciaRegistro = lote.Trailer;
                }
                else if (tipoRegistro == "9") //Trailer de Arquivo
                {
                    continue; //É automático
                }

                var listaCampos = instanciaRegistro
                    .GetType()
                    .GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(CampoCNABAttribute), true).Any())
                    .Where(p => !((CampoCNABAttribute)p.GetCustomAttributes(typeof(CampoCNABAttribute), true)[0]).Desabilitado)
                    .OrderBy(p => ((CampoCNABAttribute)p.GetCustomAttributes(typeof(CampoCNABAttribute), true)[0]).Posicao)
                    .ToList();

                instanciaRegistro.Registro = linha;

                foreach (var campo in listaCampos)
                {
                    var campoCNAB = ((CampoCNABAttribute)campo.GetCustomAttributes(typeof(CampoCNABAttribute), true)[0]);

                    if (!campo.CanWrite || !campo.GetSetMethod(/*nonPublic*/ true).IsPublic)
                        continue;

                    object valueObject = null;

                    if (campo.PropertyType == typeof(string))
                        valueObject = linha.Substring(campoCNAB.Posicao-1, campoCNAB.Tamanho).Trim();
                    else if (campo.PropertyType.IsEnum)
                        valueObject = Enum.ToObject(campo.PropertyType, Convert.ToInt32(linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho)));
                    else if (campo.PropertyType == typeof(int) || campo.PropertyType == typeof(int?) || campo.PropertyType == typeof(long) || campo.PropertyType == typeof(long?))
                        valueObject = Convert.ChangeType(linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho), campo.PropertyType);
                    else if (campo.PropertyType == typeof(decimal) || campo.PropertyType == typeof(decimal?))
                        valueObject = (decimal)Convert.ChangeType(linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho), campo.PropertyType) / 100;
                    else if (campo.PropertyType == typeof(DateTime) || campo.PropertyType == typeof(DateTime?))
                    {
                        if (string.IsNullOrEmpty(campoCNAB.Formatacao) || campoCNAB.Formatacao == "ddMMyyyy")
                        {
                            var value = linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho);

                            if (value == "00000000" || value == "        ")
                            {
                                valueObject = (DateTime?)null;
                            }
                            else
                            {
                                value = value.Substring(0, 2) + "/" + value.Substring(2, 2) + "/" + value.Substring(4, 4);

                                valueObject = DateTime.Parse(value);
                            }
                        } 
                        else if (campoCNAB.Formatacao == "HHmmss")
                        {
                            var value = linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho);

                            if (value == "000000" || value == "      ")
                            {
                                valueObject = (DateTime?)null;
                            }
                            else
                            {
                                value = value.Substring(0, 2) + ":" + value.Substring(2, 2) + ":" + value.Substring(4, 2);

                                valueObject = DateTime.Parse(value);
                            }
                        }
                    }

                    campo.SetValue(instanciaRegistro, valueObject);
                }
            }
        }

        #endregion
    }
}

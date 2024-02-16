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
using BancoBr.Common.Instances;
using BancoBr.Common.Interfaces.CNAB;
using Banco = BancoBr.CNAB.Base.Banco;

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
            arquivo.AppendLine("");

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
            #region :: Lendo o Arquivo de Retorno .::

            Lote lote = null;

            foreach (var linha in linhas)
            {
                #region ::. Criando os Registros .::

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
                    var tipoLancamento = ((HeaderLote)lote.Header).TipoLancamento;
                    var tipoSegmento = linha.Substring(13, 1);

                    switch (tipoSegmento)
                    {
                        case "A":
                            instanciaRegistro = cnab.Banco.NovoSegmentoA(tipoLancamento);
                            break;
                        case "B":
                            instanciaRegistro = cnab.Banco.NovoSegmentoB(tipoLancamento);
                            break;
                        case "C":
                            instanciaRegistro = cnab.Banco.NovoSegmentoC(tipoLancamento);
                            break;
                        case "J":
                                if (linha.Substring(14, 3) == "   " && linha.Substring(17, 2) == "52")
                                    instanciaRegistro = cnab.Banco.NovoSegmentoJ52(tipoLancamento);
                                else
                                    instanciaRegistro = cnab.Banco.NovoSegmentoJ(tipoLancamento);
                                break;
                    }

                    if (instanciaRegistro == null)
                        continue;

                    lote.Detalhe.Add(instanciaRegistro as RegistroDetalheBase);
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
                        valueObject = linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho).Trim();
                    else if (campo.PropertyType.IsEnum)
                        valueObject = Enum.ToObject(campo.PropertyType, Convert.ToInt32(linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho)));
                    else if (campo.PropertyType == typeof(int) || campo.PropertyType == typeof(int?) || campo.PropertyType == typeof(long) || campo.PropertyType == typeof(long?))
                        valueObject = Convert.ChangeType("0" + linha.Substring(campoCNAB.Posicao - 1, campoCNAB.Tamanho), campo.PropertyType);
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

                #endregion

                #region ::. Validando o Arquivo de Retorno com o Banco e Correntista Informado .::

                if (tipoRegistro == "0") //Header de Arquivo
                {
                    var headerArquivo = instanciaRegistro as HeaderArquivo;

                    if (headerArquivo.CodigoBanco != cnab.Banco.Codigo)
                        throw new Exception("O banco informado é diferente do banco no arquivo de retorno!");

                    if (
                        headerArquivo.InscricaoEmpresa != Convert.ToInt64(cnab.Correntista.CPF_CNPJ.JustNumbers()) ||
                        headerArquivo.Convenio != cnab.Correntista.Convenio ||
                        headerArquivo.NumeroAgencia != cnab.Correntista.NumeroAgencia ||
                        headerArquivo.NumeroConta != cnab.Correntista.NumeroConta
                    )
                        throw new Exception("O correntista informado é diferente do correntista no arquivo de retorno!");
                }

                #endregion
            }

            #endregion

            #region ::. Criando os Movimentos .::

            var criaMovimento = new Func<HeaderLote, Movimento>(headerLote =>
            {
                var movimento = new Movimento
                {
                    Favorecido = new Favorecido(),
                    TipoLancamento = headerLote.TipoLancamento
                };

                switch (headerLote.TipoLancamento)
                {
                    case TipoLancamentoEnum.CreditoContaMesmoBanco:
                    case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                    case TipoLancamentoEnum.OrdemPagamento:
                    case TipoLancamentoEnum.TEDMesmaTitularidade:
                    case TipoLancamentoEnum.TEDOutraTitularidade:
                        movimento.MovimentoItem = new MovimentoItemTransferenciaTED();
                        break;
                    case TipoLancamentoEnum.PIXTransferencia:
                        movimento.MovimentoItem = new MovimentoItemTransferenciaPIX();
                        break;
                    case TipoLancamentoEnum.LiquidacaoProprioBanco:
                    case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                        movimento.MovimentoItem = new MovimentoItemPagamentoTituloCodigoBarra();
                        break;
                }

                cnab.Movimentos.Add(movimento);

                return movimento;
            });

            foreach (var loteRet in cnab.Lotes)
            {
                var loteTipado = loteRet.Header as HeaderLote;

                var movimento = new Movimento();

                foreach (var segmento in loteRet.Detalhe)
                {
                    if (segmento is SegmentoA_Transferencia segmentoATransferencia)
                    {
                        movimento = criaMovimento(loteTipado);

                        movimento.Favorecido.Nome = segmentoATransferencia.NomeFavorecido;
                        movimento.TipoMovimento = segmentoATransferencia.TipoMovimento;
                        movimento.CodigoInstrucao = segmentoATransferencia.CodigoInstrucaoMovimento;
                        movimento.Moeda = segmentoATransferencia.TipoMoeda;
                        movimento.QuantidadeMoeda = segmentoATransferencia.QuantidadeMoeda;
                        movimento.DataPagamento = segmentoATransferencia.DataPagamento;
                        movimento.NumeroDocumento = segmentoATransferencia.NumeroDocumentoEmpresa;
                        movimento.ValorPagamento = segmentoATransferencia.ValorPagamento;
                        movimento.Ocorrencias = segmentoATransferencia.ListaOcorrenciasRetorno;

                        var movItem = movimento.MovimentoItem as MovimentoItemTransferenciaTED;

                        switch (cnab.Banco.Codigo)
                        {
                            default:
                                movItem.TipoConta = TipoContaEnum.ContaCorrente;
                                break;
                            case 237:
                                movItem.TipoConta = segmentoATransferencia.CodigoFinalidadeComplementar == "CC" ? TipoContaEnum.ContaCorrente : TipoContaEnum.ContaPoupanca;
                                break;
                        }

                        movItem.Banco = segmentoATransferencia.BancoFavorecido;
                        movItem.NumeroAgencia = segmentoATransferencia.AgenciaFavorecido;
                        movItem.DVAgencia = segmentoATransferencia.DVAgenciaFavorecido;
                        movItem.NumeroConta = segmentoATransferencia.ContaFavorecido;
                        movItem.DVConta = segmentoATransferencia.DVContaFavorecido;

                        if (!string.IsNullOrWhiteSpace(segmentoATransferencia.DVAgenciaContaFavorecido))
                            movItem.DVConta += segmentoATransferencia.DVAgenciaContaFavorecido;

                        movItem.CodigoFinalidadeTED = segmentoATransferencia.CodigoFinalidadeTED;
                    }
                    else if (segmento is SegmentoA_PIX segmentoAPIX)
                    {
                        movimento = criaMovimento(loteTipado);

                        movimento.Favorecido.Nome = segmentoAPIX.NomeFavorecido;
                        movimento.TipoMovimento = segmentoAPIX.TipoMovimento;
                        movimento.CodigoInstrucao = segmentoAPIX.CodigoInstrucaoMovimento;
                        movimento.Moeda = segmentoAPIX.TipoMoeda;
                        movimento.QuantidadeMoeda = segmentoAPIX.QuantidadeMoeda;
                        movimento.DataPagamento = segmentoAPIX.DataPagamento;
                        movimento.NumeroDocumento = segmentoAPIX.NumeroDocumentoEmpresa;
                        movimento.ValorPagamento = segmentoAPIX.ValorPagamento;
                        movimento.Ocorrencias = segmentoAPIX.ListaOcorrenciasRetorno;
                    }
                    else if (segmento is SegmentoB_Transferencia segmentoBTransferencia)
                    {
                        movimento.Favorecido.TipoPessoa = segmentoBTransferencia.TipoInscricaoFavorecido;
                        movimento.Favorecido.CPF_CNPJ = segmentoBTransferencia.InscricaoFavorecido.ToString();
                        movimento.Favorecido.Endereco = segmentoBTransferencia.EnderecoFavorecido;
                        movimento.Favorecido.NumeroEndereco = segmentoBTransferencia.NumeroEnderecoFavorecido;
                        movimento.Favorecido.ComplementoEndereco = segmentoBTransferencia.ComplementoEnderecoFavorecido;
                        movimento.Favorecido.CEP = segmentoBTransferencia.CEPFavorecido;
                        movimento.Favorecido.Bairro = segmentoBTransferencia.BairroFavorecido;
                        movimento.Favorecido.Cidade = segmentoBTransferencia.CidadeFavorecido;
                        movimento.Favorecido.UF = segmentoBTransferencia.UFFavorecido;
                    }
                    else if (segmento is SegmentoB_PIX segmentoBPIX)
                    {
                        movimento.Favorecido.TipoPessoa = segmentoBPIX.TipoInscricaoFavorecido;
                        movimento.Favorecido.CPF_CNPJ = segmentoBPIX.InscricaoFavorecido.ToString();

                        var movItem = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
                        movItem.TipoChavePIX = segmentoBPIX.FormaIniciacao;
                        movItem.ChavePIX = segmentoBPIX.ChavePIX;
                    }
                    else if (segmento is SegmentoJ segmentoJ)
                    {
                        movimento = criaMovimento(loteTipado);

                        movimento.Favorecido.Nome = segmentoJ.NomeBeneficiario;
                        movimento.TipoMovimento = segmentoJ.TipoMovimento;
                        movimento.CodigoInstrucao = segmentoJ.CodigoInstrucaoMovimento;
                        movimento.Moeda = segmentoJ.CodigoMoeda == 9 ? "BRL" : "";
                        movimento.QuantidadeMoeda = segmentoJ.QuantidadeMoeda;
                        movimento.DataPagamento = segmentoJ.DataPagamento;
                        movimento.NumeroDocumento = segmentoJ.CodigoDocumentoNaEmpresa;
                        movimento.ValorPagamento = segmentoJ.ValorPagamento;
                        movimento.Ocorrencias = segmentoJ.ListaOcorrenciasRetorno;

                        var movItem = movimento.MovimentoItem as MovimentoItemPagamentoTituloCodigoBarra;
                        movItem.BancoCodigoBarra = segmentoJ.BancoCodigoBarra;
                        movItem.MoedaCodigoBarra = segmentoJ.MoedaCodigoBarra;
                        movItem.DVCodigoBarra = segmentoJ.DVCodigoBarra;
                        movItem.FatorVencimentoCodigoBarra = segmentoJ.FatorVencimentoCodigoBarra;
                        movItem.ValorCodigoBarra = segmentoJ.ValorCodigoBarra;
                        movItem.CampoLivreCodigoBarra = segmentoJ.CampoLivreCodigoBarra;
                        movItem.Desconto = segmentoJ.ValorDesconto;
                        movItem.Acrescimo = segmentoJ.ValorAcrescimo;
                    }
                    else if (segmento is SegmentoJ52_Boleto segmentoJ52Boleto)
                    {
                        movimento.Favorecido.TipoPessoa = segmentoJ52Boleto.TipoInscricaoCedente;
                        movimento.Favorecido.CPF_CNPJ = segmentoJ52Boleto.InscricaoCedente.ToString();
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}

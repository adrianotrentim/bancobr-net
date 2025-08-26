using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Attributes;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;

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

        public static void Importar(this ArquivoCNAB cnab, Stream linhasStream)
        {
            var linhas = new List<string>();

            using (var sr = new StreamReader(linhasStream))
                while (!sr.EndOfStream)
                    linhas.Add(sr.ReadLine());

            Importar(cnab, linhas.AsEnumerable());
        }

        public static void Importar(this ArquivoCNAB cnab, IEnumerable<string> linhas)
        {
            #region :: Lendo o Arquivo de Retorno .::

            cnab.Arquivo = string.Join("\r\n", linhas);

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
                    if (!new []{20, 22, 30}.Contains(Convert.ToInt32(linha.Substring(9, 2)))) // 20 = Somente Pagamento de Fornecedores, 22 = Tributos, 30 = Salários, DDA (Código 3) será desenvolvido posteriormente
                        continue;

                    var tipoLancamento = Convert.ToInt32(linha.Substring(11, 2));

                    lote = new Lote
                    {
                        Header = cnab.Banco.NovoHeaderLote((TipoLancamentoEnum)tipoLancamento)
                    };

                    cnab.Lotes.Add(lote);

                    instanciaRegistro = lote.Header;
                }
                else if (tipoRegistro == "2") //Detalhe Iniciais de Lote
                {

                }
                else if (tipoRegistro == "3") //Detalhe Detalhe
                {
                    if (lote?.Header == null) //Se não criou o lote ou o header, é porque os registros do segmento ainda não foram desenvolvidos.
                        continue;

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
                        case "J":
                            if ((linha.Substring(14, 1) == " " || linha.Substring(14, 1) == "0") && linha.Substring(17, 2) == "52")
                                instanciaRegistro = cnab.Banco.NovoSegmentoJ52(tipoLancamento);
                            else
                                instanciaRegistro = cnab.Banco.NovoSegmentoJ(tipoLancamento);
                            break;
                        case "O":
                            instanciaRegistro = cnab.Banco.NovoSegmentoO(tipoLancamento);
                            break;
                    }

                    if (instanciaRegistro == null)
                        continue;

                    lote.Detalhe.Add(instanciaRegistro as RegistroDetalheBase);
                }
                else if (tipoRegistro == "4") //Detalhe Finais de Lote
                {
                    if (lote?.Header == null) //Se não criou o lote ou o header, é porque os registros do segmento ainda não foram desenvolvidos.
                        continue;
                }
                else if (tipoRegistro == "5") //Trailer de Lote
                {
                    if (lote?.Header == null) //Se não criou o lote ou o header, é porque os registros do segmento ainda não foram desenvolvidos.
                        continue;

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

                    cnab.Banco.Empresa = new Correntista
                    {
                        TipoPessoa = headerArquivo.TipoInscricaoCpfcnpj,
                        CPF_CNPJ = headerArquivo.InscricaoEmpresa.ToString(),
                        Nome = headerArquivo.NomeEmpresa,
                        Banco = headerArquivo.CodigoBanco,
                        Convenio = headerArquivo.Convenio,
                        NumeroAgencia = headerArquivo.NumeroAgencia,
                        DVAgencia = headerArquivo.DVAgencia,
                        NumeroConta = headerArquivo.NumeroConta,
                        DVConta = headerArquivo.DVConta + (string.IsNullOrWhiteSpace(headerArquivo.DVAgenciaConta) ? "" : headerArquivo.DVAgenciaConta)
                    };
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
                    case TipoLancamentoEnum.PagamentoTributosCodigoBarra:
                        movimento.MovimentoItem = new MovimentoItemPagamentoConvenioCodigoBarra();
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
                    if (segmento is SegmentoA segmentoA)
                    {
                        movimento = criaMovimento(loteTipado);

                        movimento.Registro = segmento.Registro;
                        movimento.Favorecido.Nome = segmentoA.NomeFavorecido;
                        movimento.TipoMovimento = segmentoA.TipoMovimento;
                        movimento.CodigoInstrucao = segmentoA.CodigoInstrucaoMovimento;
                        movimento.Moeda = segmentoA.TipoMoeda;
                        movimento.QuantidadeMoeda = segmentoA.QuantidadeMoeda;
                        movimento.DataPagamento = segmentoA.DataPagamento;
                        movimento.NumeroDocumento = segmentoA.NumeroDocumentoEmpresa;
                        movimento.NumeroDocumentoNoBanco = segmentoA.NumeroDocumentoBanco;
                        movimento.ValorPagamento = segmentoA.ValorPagamento;
                        movimento.Ocorrencias = segmentoA.ListaOcorrenciasRetorno;

                        switch (loteTipado.TipoLancamento)
                        {
                            case TipoLancamentoEnum.TEDMesmaTitularidade:
                            case TipoLancamentoEnum.TEDOutraTitularidade:
                            case TipoLancamentoEnum.CreditoContaMesmoBanco:
                            case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:

                                var movItem = movimento.MovimentoItem as MovimentoItemTransferenciaTED;

                                switch (cnab.Banco.Codigo)
                                {
                                    default:
                                        movItem.TipoConta = TipoContaEnum.ContaCorrente;
                                        break;
                                    case 237:
                                        movItem.TipoConta = segmentoA.CodigoFinalidadeComplementar == "CC" ? TipoContaEnum.ContaCorrente : TipoContaEnum.ContaPoupanca;
                                        break;
                                }

                                movItem.Banco = ((SegmentoA_Transferencia)segmentoA).BancoFavorecido;
                                movItem.NumeroAgencia = ((SegmentoA_Transferencia)segmentoA).AgenciaFavorecido;
                                movItem.DVAgencia = ((SegmentoA_Transferencia)segmentoA).DVAgenciaFavorecido;
                                movItem.NumeroConta = ((SegmentoA_Transferencia)segmentoA).ContaFavorecido;
                                movItem.DVConta = ((SegmentoA_Transferencia)segmentoA).DVContaFavorecido;

                                if (!string.IsNullOrWhiteSpace(((SegmentoA_Transferencia)segmentoA).DVAgenciaContaFavorecido))
                                    movItem.DVConta += ((SegmentoA_Transferencia)segmentoA).DVAgenciaContaFavorecido;

                                movItem.CodigoFinalidadeTED = ((SegmentoA_Transferencia)segmentoA).CodigoFinalidadeTED;

                                break;

                            case TipoLancamentoEnum.PIXTransferencia:
                            case TipoLancamentoEnum.PIXQrCode:

                                break;
                        }
                    }
                    else if (segmento is SegmentoB segmentoB)
                    {
                        movimento.Favorecido.TipoPessoa = segmentoB.TipoInscricaoFavorecido;
                        movimento.Favorecido.CPF_CNPJ = segmentoB.InscricaoFavorecido.ToString();

                        switch (loteTipado.TipoLancamento)
                        {
                            case TipoLancamentoEnum.TEDMesmaTitularidade:
                            case TipoLancamentoEnum.TEDOutraTitularidade:
                            case TipoLancamentoEnum.CreditoContaMesmoBanco:
                            case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:

                                movimento.Favorecido.Endereco = ((SegmentoB_Transferencia)segmentoB).EnderecoFavorecido;
                                movimento.Favorecido.NumeroEndereco = ((SegmentoB_Transferencia)segmentoB).NumeroEnderecoFavorecido;
                                movimento.Favorecido.ComplementoEndereco = ((SegmentoB_Transferencia)segmentoB).ComplementoEnderecoFavorecido;
                                movimento.Favorecido.CEP = ((SegmentoB_Transferencia)segmentoB).CEPFavorecido;
                                movimento.Favorecido.Bairro = ((SegmentoB_Transferencia)segmentoB).BairroFavorecido;
                                movimento.Favorecido.Cidade = ((SegmentoB_Transferencia)segmentoB).CidadeFavorecido;
                                movimento.Favorecido.UF = ((SegmentoB_Transferencia)segmentoB).UFFavorecido;

                                break;

                            case TipoLancamentoEnum.PIXTransferencia:
                            case TipoLancamentoEnum.PIXQrCode:

                                var movItem = movimento.MovimentoItem as MovimentoItemTransferenciaPIX;
                                movItem.TipoChavePIX = ((SegmentoB_PIX)segmentoB).FormaIniciacao;
                                movItem.ChavePIX = ((SegmentoB_PIX)segmentoB).ChavePIX;

                                break;
                        }
                    }
                    else if (segmento is SegmentoJ segmentoJ)
                    {
                        movimento = criaMovimento(loteTipado);

                        movimento.Registro = segmento.Registro;
                        movimento.Favorecido.Nome = segmentoJ.NomeBeneficiario;
                        movimento.TipoMovimento = segmentoJ.TipoMovimento;
                        movimento.CodigoInstrucao = segmentoJ.CodigoInstrucaoMovimento;
                        movimento.Moeda = segmentoJ.CodigoMoeda == 9 ? "BRL" : "";
                        movimento.QuantidadeMoeda = segmentoJ.QuantidadeMoeda;
                        movimento.DataPagamento = segmentoJ.DataPagamento;
                        movimento.NumeroDocumento = segmentoJ.NumeroDocumentoEmpresa;
                        movimento.NumeroDocumentoNoBanco = segmentoJ.NumeroDocumentoBanco;
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
                    else if (segmento is SegmentoO segmentoO)
                    {
                        movimento = criaMovimento(loteTipado);

                        movimento.Registro = segmento.Registro;
                        movimento.Favorecido.Nome = segmentoO.NomeBeneficiario;
                        movimento.TipoMovimento = segmentoO.TipoMovimento;
                        movimento.CodigoInstrucao = segmentoO.CodigoInstrucaoMovimento;
                        movimento.DataPagamento = segmentoO.DataPagamento;
                        movimento.NumeroDocumento = segmentoO.NumeroDocumentoEmpresa;
                        movimento.NumeroDocumentoNoBanco = segmentoO.NumeroDocumentoBanco;
                        movimento.ValorPagamento = segmentoO.ValorPagamento;
                        movimento.Ocorrencias = segmentoO.ListaOcorrenciasRetorno;

                        var movItem = movimento.MovimentoItem as MovimentoItemPagamentoConvenioCodigoBarra;
                        movItem.CodigoBarra = segmentoO.CodigoBarra;
                        movItem.DataVencimento = segmentoO.DataVencimento;
                        
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}

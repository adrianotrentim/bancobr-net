﻿using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.Common.Core
{
    public static class CodigoOcorrenciasRetorno
    {
        public static List<Ocorrencia> Ocorrencias => new List<Ocorrencia>
        {
            new Ocorrencia("00", "Crédito ou Débito Efetivado", "Compromisso efetivamente pago/liquidado. Este código indica que o pagamento foi confirmado"),
            new Ocorrencia("01", "Insuficiência de Fundos - Débito Não Efetuado", "Seu pagamento não pode ser efetivado por não possuir saldo disponível suficiente em sua conta corrente."),
            new Ocorrencia("02", "Crédito ou Débito Cancelado pelo Pagador/Credor", "Compromisso agendado foi cancelado."),
            new Ocorrencia("03", "Débito Autorizado pela Agência - Efetuado", "Pagamento foi autorizado e efetivado pela agência"),
            new Ocorrencia("AA", "Controle Inválido", "Trata-se de movimento processado com Data e Hora de Gravação de outro movimento já processado pelo Sistema (Arquivo duplicado).\r\nCampos de controle do arquivo de Remessa (Banco, Lote, Registro) inválidos."),
            new Ocorrencia("AB", "Tipo de Operação Inválido", "Verificar coluna '9' do header de lote. Para pagamento a fornecedor, tributos e pagamento de títulos, devera conter fixo 'C'. \r\nVerificar as posições 223 a 224 do header de lote devera conter o parâmetro fixo '01'."),
            new Ocorrencia("AC", "Tipo de Serviço Inválido", "Código do tipo serviço diferente dos utilizados, ou tipo de serviço incompatível à forma de pagamento. \r\nVerificar serviço informado nas posições de 10 a 11 no header de lote (no layout G025)"),
            new Ocorrencia("AD", "Forma de Lançamento Inválida", "Código do tipo serviço diferente dos utilizados, ou tipo de serviço incompatível à forma de pagamento. \r\nVerificar serviço informado nas posições de 10 a 11 no header de lote (no layout G025)"),
            new Ocorrencia("AE", "Tipo/Número de Inscrição Inválido", "Verificar a posição 18 nas linhas de header de arquivo e header de lote o campo tipo de inscrição (1- CPF, 2 - CNPJ, 3 - PIS, 9 - Outros).\r\nDas posições de 19 a 32 se foi preenchido conforme o tipo de inscrição informado."),
            new Ocorrencia("AF", "Código de Convênio Inválido", "Verificar nas linhas de header de arquivo e header de lote, da posição 33 a 52 se esta preenchida com o código de convênio conforme cadastro junto ao Banco."),
            new Ocorrencia("AG", "Agência/Conta Corrente/DV Inválido", "Verificar agência e conta de débito nas linhas de header de arquivo e header de lote o código da agência nas posições de 53 a 57 (mantendo “0” a esquerda. Ex.: 01111) dígito da agência deve ser informado na posição 58, número da conta das posições de 59 a 70 com dígito na posição 71. \r\nPara mais detalhes verificar no layout G008, G009, G010 e G011."),
            new Ocorrencia("AH", "Nº Seqüencial do Registro no Lote Inválido", "Verificar as posições de 9 a 13 em cada segmento do arquivo, a sequência deve ser numérica e em ordem crescente (Ex: 00001, 00002...), o sequencial deve começar sempre em 00001 em cada novo lote. \r\nPara mais detalhes verificar no layout G038."),
            new Ocorrencia("AI", "Código de Segmento de Detalhe Inválido", "A sequencia deve ser numérica e ordem crescente (ex: 00001, 00002 ...) o sequencial deve começar sempre em 00001 em cada novo lote."),
            new Ocorrencia("AJ", "Tipo de Movimento Inválido", "Verificar qual o código do tipo de movimento foi informado na posição 15 em cada um dos segmentos do arquivo. \r\nNota: '0'= Inclusão, '5'= Alteração, '9'= Exclusão. Demais códigos e detalhes verificar no layout G060. \r\nVerificar se o tópico de receita está zerado na posição de 111 a 116 na remessa para o Segmento N. Verificar se o código de barras desse tributo já não foi pago anteriormente."),
            new Ocorrencia("AK", "Código da Câmara de Compensação do Banco Favorecido/Depositário Inválido", "Preencher com o código da Câmara Centralizadora para envio. '018' para TED (STR, CIP), '700' para DOC (COMPE).\r\nOutras modalidades preencher com zeros. Nas linhas de segmento 'A' do arquivo coluna 18 a 20."),
            new Ocorrencia("AL", "Código do Banco Favorecido, Instituição de Pagamento ou Depositário Inválido", "Banco favorecido informado está inválido, verificar nas posições de 21 a 23 nas linhas de segmento 'A'.\r\nCampos devem ser numéricos. Para mais detalhes verificar no layout P002"),
            new Ocorrencia("AM", "Agência Mantenedora da Conta Corrente do Favorecido Inválida", "Verificar numero da agência do favorecido, poderá estar invalido ou deslocado nas linhas de detalhe no segmento 'A', verificar nas posições de 24 a 28 sendo o digito verificador da agencia na posição 29."),
            new Ocorrencia("AN", "Conta Corrente/DV/Conta de Pagamento do Favorecido Inválido", "Número da conta do favorecido poderá estar inválido ou deslocado nas linhas de detalhe no segmento 'A', verificar nas posições de 30 a 41 sendo o digito verificador na posição 42.\r\nNota: conta do favorecido também poderá estar encerrada ou bloqueada"),
            new Ocorrencia("AO", "Nome do Favorecido Não Informado", "Nome do credor/favorecido está totalmente em branco e o mesmo é obrigatório para esse pagamento."),
            new Ocorrencia("AP", "Data Lançamento Inválido", "Pode ser um dos seguintes motivos:\r\n\r\nO campo Data (vencimento, lançamento, emissão ou processamento) está zerado, em branco, fora do padrão (DDMMAAAA) ou não numérico;\r\nA data informada é inferior à data base (data da leitura/processamento do arquivo), ou é igual à data base para pagamento via \"Crédito Administrativo\";\r\nA solicitação para cancelamento do compromisso a ser pago está fora do prazo limite;\r\nA data de desconto informada está incorreta ou maior que a data do vencimento do título;\r\nO horário do agendamento/liberação do compromisso ultrapassou o horário limite para efetuar consulta de saldo em sua conta corrente para pagamento do compromisso;\r\nO horário de inclusão ou liberação da TED ou Títulos de Outros Bancos 250K ultrapassou o horário limite para envio;\r\nO pagamento de contas, tributos e impostos não pode ser realizado por não ser permitido seu recebimento para esta data. Deverá contatar com o emissor da fatura para nova emissão;\r\nPara qualquer uma das ocorrências o compromisso deverá ser incluído novamente, porém, com a devida regularização;"),
            new Ocorrencia("AQ", "Tipo/Quantidade da Moeda Inválido", "Código do tipo de moeda diferente das utilizadas ou quantidade de moeda não numérica ou zerada."),
            new Ocorrencia("AR", "Valor do Lançamento Inválido", "No segmento 'A' do arquivo de remessa verificar nas posições de 120 a 134 (valor do Pagamento), e nas posições de 163 a 177 (valor real da efetivação do pagamento). \r\nNo segmento 'J' do arquivo de remessa verificar nas posições de 153 a 167, o valor informado para pagamento. \r\nNo segmento 'O' do arquivo de remessa verificar das colunas 108 a 122, o valor informado para pagamento. No segmento 'N' do arquivo de remessa verificar das colunas 96 a 110, o valor informado para pagamento. \r\nPara mais detalhes verificar no layout P010 para os segmentos A ,J e N e P004 para segmento O."),
            new Ocorrencia("AS", "Aviso ao Favorecido - Identificação Inválida"),
            new Ocorrencia("AT", "Tipo/Número de Inscrição do Favorecido Inválido", "Verificar no segmento 'B' do arquivo remessa: Tipo de inscrição do favorecido na posição 18 sendo: (1- CPF, 2 - CNPJ, 3 - PIS, 9 - Outros), e nas posições de 19 a 32 se o preenchimento foi realizado de acordo com o tipo informado na posição 18. \r\nPara mais detalhes verificar nos layouts G005 e G006."),
            new Ocorrencia("AU", "Logradouro do Favorecido Não Informado", "Verificar no segmento 'B' do arquivo de remessa se nas posições de 33 a 62 contém o endereço do favorecido. \r\nPara mais detalhes verificar no layout G032."),
            new Ocorrencia("AV", "Nº do Local do Favorecido Não Informado", "Verificar no segmento 'B' do arquivo de remessa nas posições de 63 a 67, se foi informado o numero do local referente ao endereço do favorecido. \r\nPara mais detalhes verificar no layout G032."),
            new Ocorrencia("AW", "Cidade do Favorecido Não Informada", "Verificar no segmento 'B' do arquivo de remessa nas posições de 98 a 117, se foi informado o nome da cidade do favorecido. \r\nPara mais detalhes verificar no layout G033."),
            new Ocorrencia("AX", "CEP/Complemento do Favorecido Inválido", "Verificar no segmento 'B' do arquivo de remessa nas posições de 118 a 123, se foi informado o CEP do favorecido e nas posições de 123 a 125 se foi informado o complemento do CEP."),
            new Ocorrencia("AY", "Sigla do Estado do Favorecido Inválida", "Verificar no segmento 'B' do arquivo de remessa nas posições de 126 a 127, se foi informado à sigla do estado/UF do favorecido. \r\nPara mais detalhes verificar no layout G036."),
            new Ocorrencia("AZ", "Código/Nome do Banco Depositário Inválido", "Código do Banco favorecido encontra-se inválido, não numérico ou zerado."),
            new Ocorrencia("BA", "Código/Nome da Agência Depositária Não Informado", "Ocorrência para títulos rastreados ou DDA."),
            new Ocorrencia("BB", "Seu Número Inválido", "Inclusão de um compromisso que já se encontra no banco (compromisso em duplicidade)."),
            new Ocorrencia("BC", "Nosso Número Inválido", "O Nosso Número identificado para quitação de títulos encontra-se irregular."),
            new Ocorrencia("BD", "Inclusão Efetuada com Sucesso", "Pagamento agendado com sucesso, o mesmo pode esta autorizado ou desautorizado, na base do Banco."),
            new Ocorrencia("BE", "Alteração Efetuada com Sucesso", "Autorização de pagamento que esta na base do Banco Autorizado ou Desautorizado, via arquivo."),
            new Ocorrencia("BF", "Exclusão Efetuada com Sucesso", "Pagamento excluído com êxito"),
            new Ocorrencia("BG", "Agência/Conta Impedida Legalmente/Bloqueada", "A conta de crédito informada encontra-se impedida por determinação de meios legais, que impossibilitam a efetivação do pagamento."),
            new Ocorrencia("BH", "Correntista não pagou salário"),
            new Ocorrencia("BI", "Falecimento do mutuário"),
            new Ocorrencia("BJ", "Correntista não enviou remessa do mutuário"),
            new Ocorrencia("BK", "Correntista não enviou remessa no vencimento"),
            new Ocorrencia("BL", "Valor da parcela inválida"),
            new Ocorrencia("BM", "Identificação do contrato inválida"),
            new Ocorrencia("BN", "Operação de Consignação Incluída com Sucesso"),
            new Ocorrencia("BO", "Operação de Consignação Alterada com Sucesso"),
            new Ocorrencia("BP", "Operação de Consignação Excluída com Sucesso"),
            new Ocorrencia("BQ", "Operação de Consignação Liquidada com Sucesso"),
            new Ocorrencia("BR", "Reativação Efetuada com Sucesso"),
            new Ocorrencia("BS", "Suspensão Efetuada com Sucesso"),
            new Ocorrencia("CA", "Código de Barras - Código do Banco Inválido", "Verificar nas posições de 18 a 61 no segmento J se o código de barras está invalido, deslocado ou em branco."),
            new Ocorrencia("CB", "Código de Barras - Código da Moeda Inválido", "Verificar nas posições de 18 a 61 no segmento J se o código de barras está invalido, deslocado ou em branco. \r\nVerificar também nas posições de 18 a 20 se código do banco está correto (Ex.\u00b0: Bradesco: 237). \r\nNota: campo deve ser apenas NUMERICO. Para mais detalhes verificar layout G063."),
            new Ocorrencia("CC", "Código de Barras - Dígito Verificador Geral Inválido", "Informar ao cliente que o código de barras no segmento 'J' informado nas posições de 18 a 61 está invalido ou deslocado. \r\nVerificar também o se o digito geral está na posição 22 do código de barras. Nota: campo deve ser apenas NUMERICO. \r\nPara mais detalhes verificar no layout G063 'CD' = Código de Barras - Valor do Título Divergente/Inválido.\r\nVerificar nas posições de 18 a 61 no segmento J se o código de barras está invalido, deslocado ou em branco. \r\nVerificar também nas posições de 27 a 36 se o valor do código de barras está correto em relação ao valor do documento. \r\nNota: campo deve ser apenas numérico. \r\nPara mais detalhes verificar layout G063."),
            new Ocorrencia("CD", "Código de Barras - Valor do Título Inválido"),
            new Ocorrencia("CE", "Código de Barras - Campo Livre Inválido", "Verificar nas posições de 18 a 61 no segmento J se o código de barras está invalido, deslocado ou em branco. \r\nVerificar também nas posições de 37 a 61 se dados estão em conformidade com o campo livre do código de barras. \r\nNota: campo deve ser apenas numérico. Para mais detalhes verificar layout G063."),
            new Ocorrencia("CF", "Valor do Documento Inválido", "Verificar nas posições de 100 a 114 do arquivo de remessa no segmento 'J' se o valor do titulo está inválido ou deslocado. \r\nO campo deve ser apenas numérico. Para mais detalhes verificar no layout G042."),
            new Ocorrencia("CG", "Valor do Abatimento Inválido", "Verificar nas posições de 115 a 129 no segmento 'J' se o valor do abatimento está inválido ou deslocado. \r\nO campo deve ser apenas numérico. Para mais detalhes verificar no layout L002."),
            new Ocorrencia("CH", "Valor do Desconto Inválido", "Verificar nas posições de 115 a 129 no segmento 'J' se o valor do desconto/bonificação está inválido ou deslocado. \r\nO campo deve ser apenas numérico. Para mais detalhes verificar no layout L002"),
            new Ocorrencia("CI", "Valor de Mora Inválido", "Verificar nas posições de 130 a 144 no segmento 'J' se o valor de mora está inválido ou deslocado.\r\nO campo deve ser apenas numérico. \r\nPara mais detalhes verificar no layout L003."),
            new Ocorrencia("CJ", "Valor da Multa Inválido", "Verificar nas posições de 130 a 144 no segmento 'J' se o valor de multa está inválido ou deslocado. \r\nO campo deve ser apenas NUMERICO. Para mais detalhes verificar no layout L003."),
            new Ocorrencia("CK", "Valor do IR Inválido", "Verificar nas posições de 18 a 32 no segmento 'C' se o valor do IR (Imposto de Renda) está inválido ou deslocado. \r\nNota: campo deve ser apenas NUMERICO. Para mais detalhes verificar no layout G050."),
            new Ocorrencia("CL", "Valor do ISS Inválido", "Verificar nas posições de 33 a 47 se o valor do ISS está inválido ou deslocado. \r\nO campo deve ser apenas NUMERICO. \r\nPara mais detalhes verificar no layout G051. "),
            new Ocorrencia("CM", "Valor do IOF Inválido", "Verificar nas posições de 48 a 62 no segmento 'C' se o valor do IOF está inválido ou deslocado.\r\nO campo deve ser apenas NUMERICO. Para mais detalhes verificar no layout G052."),
            new Ocorrencia("CN", "Valor de Outras Deduções Inválido", "Verificar nas posições de 63 a 77 no segmento 'C' se o valor de outras deduções está inválido ou deslocado. \r\nNota: campo dever ser apenas NUMERICO. Para mais detalhes verificar no layout G053."),
            new Ocorrencia("CO", "Valor de Outros Acréscimos Inválido", "Verificar nas posições de 78 a 92 no segmento 'C' se o valor de outros acréscimos está inválido ou deslocado. \r\nNota: campo dever ser apenas NUMERICO. Para mais detalhes verificar no layout G054."),
            new Ocorrencia("CP", "Valor do INSS Inválido", "Verificar nas posições de 113 a 127 no segmento 'C' se o valor do INSS está inválido ou deslocado.\r\nNota: campo deve ser apenas NUMERICO. Para mais detalhes verificar no layout G055. "),
            new Ocorrencia("HA", "Lote Não Aceito", "Trata-se de movimento já processado (Duplicado), ou com Registro/Segmento incorreto."),
            new Ocorrencia("HB", "Inscrição da Correntista Inválida para o Contrato", "Verificar no header de arquivo e no header de lote nas posições de 19 a 32 se o numero de inscrição da empresa pertence ao numero de convenio informado nas posições de 33 a 38."),
            new Ocorrencia("HC", "Convênio com a Correntista Inexistente/Inválido para o Contrato", "Verificar se o número de convenio informado nas posições de 33 a 38 está correto, verificação deve ser realizada no header de arquivo e no header de lote. \r\nPara mais detalhes verificar no layout G007.'"),
            new Ocorrencia("HD", "Agência/Conta Corrente da Correntista Inexistente/Inválido para o Contrato", "Verificar no header de arquivo e header de lote se o número de agencia e conta informada corretamente. \r\nCódigo da agencia fica nas posições de 53 a 57 sendo o digito verificador na posição 58 para Código da conta verificar nas posições de 59 a 70 sendo o digito verificador na posição 71. \r\nPara mais detalhes verificar no layout G008, G009, G010 e G011."),
            new Ocorrencia("HE", "Tipo de Serviço Inválido para o Contrato", "Verificar no header de lote qual o tipo de serviço informado nas posições de 10 a 11. \r\nSe o tipo de serviço estiver correto em relação ao lançamento do arquivo verificar se contrato está com o serviço disponível para utilização. \r\nPara mais detalhes verificar no layout G025."),
            new Ocorrencia("HF", "Conta Corrente da Correntista com Saldo Insuficiente", "Lançamento recusado por saldo insuficiente na conta de debito vinculada ao convênio utilizado."),
            new Ocorrencia("HG", "Lote de Serviço Fora de Seqüência", "Verificar se na data de transmissão, nas posições de 4 a 7 se existem arquivos com o mesmo numero de lote ou se foram enviados fora de sequencia, também verificar se o Nº Sequencial do Registro no Lote nas posições de 9 a 13 (Segmentos A, B, J, N, O) estão em sequência crescente para cada lote aberto quando for arquivo multiheader. \r\nPara mais detalhes verificar no layout G002 e G038."),
            new Ocorrencia("HH", "Lote de Serviço Inválido", "Verificar se na data de transmissão existe arquivos com o mesmo numero de lote ou se houve envio fora de sequencia (devendo iniciar em 0001). \r\nVerificar no header do lote, nas posições de 4 a 7. \r\nPara mais detalhes verificar no layout G002."),
            new Ocorrencia("HI", "Arquivo não aceito", "Todo arquivo será rejeitado por diferentes motivos de recusa nos segmentos de detalhe."),
            new Ocorrencia("HJ", "Tipo de Registro Inválido", "Verificar em todas as linhas do arquivo remessa na posição '8' se foi informado o código de registro correto. Ex.: No header do arquivo na posição '8' deve conter o código '0'que significa Header de Arquivo, para demais segmentos teremos '1' que significa Header de Lote, '2' que significa Registros Iniciais do Lote, '3' que significa Detalhe, '4' que significa Registros Finais do Lote, '5' = que significa Trailer de Lote, '9' que significa Trailer de Arquivo. \r\nPara mais detalhes verificar no layout G003."),
            new Ocorrencia("HK", "Código Remessa / Retorno Inválido", "Verificar no header do arquivo na posição 143 se foi informado o código fixo '1' que significa remessa. \r\nNota: Qualquer informação diferente de '1' pode gerar recusa. Para mais detalhes verificar no layout G015."),
            new Ocorrencia("HL", "Versão de layout inválida", "Verificar nas posições de 14 a 16 no header de lote se a versão do layout está correta em relação ao tipo de pagamento inserido no lote \r\nNota: Esse parâmetro é utilizado para que possamos saber que tipo de estrutura de pagamento deve ser lida no arquivo Ex.\u00b0: Se a Versão do layout informada for '040' sabemos que a estrutura que está no arquivo remessa deve ser para 'Títulos de Cobrança' para demais lançamentos existem as seguintes versões: PAGFOR '045', PAGAMENTO DE TITULOS '040' e TRIBUTOS '012', Verificar também no header de arquivo nas posições de 164 à 166 se está informado fixo '089'. Para mais detalhes verificar no layout G019 e G030.'HM' = Mutuário não identificado"),
            new Ocorrencia("HM", "Mutuário não identificado"),
            new Ocorrencia("HN", "Tipo do beneficio não permite empréstimo"),
            new Ocorrencia("HO", "Beneficio cessado/suspenso"),
            new Ocorrencia("HP", "Beneficio possui representante legal"),
            new Ocorrencia("HQ", "Beneficio é do tipo PA (Pensão alimentícia)"),
            new Ocorrencia("HR", "Quantidade de contratos permitida excedida"),
            new Ocorrencia("HS", "Beneficio não pertence ao Banco informado"),
            new Ocorrencia("HT", "Início do desconto informado já ultrapassado"),
            new Ocorrencia("HU", "Número da parcela inválida"),
            new Ocorrencia("HV", "Quantidade de parcela inválida"),
            new Ocorrencia("HW", "Margem consignável excedida para o mutuário dentro do prazo do contrato"),
            new Ocorrencia("HX", "Empréstimo já cadastrado"),
            new Ocorrencia("HY", "Empréstimo inexistente"),
            new Ocorrencia("HZ", "Empréstimo já encerrado"),
            new Ocorrencia("H1", "Arquivo sem trailer", "Verificar no arquivo de remessa se falta a ultima linha do registro trailer do arquivo registro tipo '9'. Nota: O tipo de registro é informado em todas as linhas do arquivo na posição 8. \r\nPara mais detalhes verificar no layout G003."),
            new Ocorrencia("H2", "Mutuário sem crédito na competência"),
            new Ocorrencia("H3", "Não descontado – outros motivos"),
            new Ocorrencia("H4", "Retorno de Crédito não pago", "Estorno de pagamento quando os dados do favorecido esta incorreto"),
            new Ocorrencia("H5", "Cancelamento de empréstimo retroativo"),
            new Ocorrencia("H6", "Outros Motivos de Glosa"),
            new Ocorrencia("H7", "Margem consignável excedida para o mutuário acima do prazo do contrato"),
            new Ocorrencia("H8", "Mutuário desligado do empregador"),
            new Ocorrencia("H9", "Mutuário afastado por licença"),
            new Ocorrencia("IA", "Primeiro nome do mutuário diferente do primeiro nome do movimento do censo ou diferente da base de Titular do Benefício"),
            new Ocorrencia("IB", "Benefício suspenso/cessado pela APS ou Sisobi"),
            new Ocorrencia("IC", "Benefício suspenso por dependência de cálculo"),
            new Ocorrencia("ID", "Benefício suspenso/cessado pela inspetoria/auditoria"),
            new Ocorrencia("IE", "Benefício bloqueado para empréstimo pelo beneficiário"),
            new Ocorrencia("IF", "Benefício bloqueado para empréstimo por TBM"),
            new Ocorrencia("IG", "Benefício está em fase de concessão de PA ou desdobramento"),
            new Ocorrencia("IH", "Benefício cessado por óbito"),
            new Ocorrencia("II", "Benefício cessado por fraude"),
            new Ocorrencia("IJ", "Benefício cessado por concessão de outro benefício"),
            new Ocorrencia("IK", "Benefício cessado: estatutário transferido para órgão de origem"),
            new Ocorrencia("IL", "Empréstimo suspenso pela APS"),
            new Ocorrencia("IM", "Empréstimo cancelado pelo banco"),
            new Ocorrencia("IN", "Crédito transformado em PAB"),
            new Ocorrencia("IO", "Término da consignação foi alterado"),
            new Ocorrencia("IP", "Fim do empréstimo ocorreu durante período de suspensão ou concessão"),
            new Ocorrencia("IQ", "Empréstimo suspenso pelo banco"),
            new Ocorrencia("IR", "Não averbação de contrato – quantidade de parcelas/competências informadas ultrapassou a data limite da extinção de cota do dependente titular de benefícios"),
            new Ocorrencia("TA", "Lote Não Aceito - Totais do Lote com Diferença", "Verificar no trailer de lote nas posições de 18 a 23 se o somatório de registros informados está preenchido e se está correto em relação ao total de linhas do lote. \r\nVerificar no trailer de lote nas posições de 24 a 41 se o total do valor do lote está correto em relação ao valor total dos pagamentos. \r\nNo trailer do arquivo nas posições de 18 a 23, verificar se a quantidade de lotes está correta ou nas posições de 24 a 29 se quantidade de registros informados está correta em relação ao total de linhas no arquivo remessa. \r\nPara mais detalhes verificar no layout G057, P007, G049 e G056."),
            new Ocorrencia("YA", "Título Não Encontrado", "O título de Cobrança não foi localizado na CIP para pagamento."),
            new Ocorrencia("YB", "Identificador Registro Opcional Inválido", "Verificar no segmento 'J-52' do arquivo de remessa nas posições de 18 a 19 se foi informado o código fixo '52'. \r\nNo segmento 'N' do arquivo de remessa quando for DARF sem código de barras, verificar nas posições de 143 a 159 se foi informado o 'número de referência', pois se trata de um campo numérico obrigatório para 'DARF sem código de barras'. \r\nPara mais detalhes verificar no layout G067 e N009."),
            new Ocorrencia("YC", "Código Padrão Inválido", "Ocorrência especifica para o tipo de serviço alegação de Pagador."),
            new Ocorrencia("YD", "Código de Ocorrência Inválido", "Ocorrência especifica para o tipo de serviço alegação de Pagador."),
            new Ocorrencia("YE", "Complemento de Ocorrência Inválido", "Ocorrência especifica para o tipo de serviço alegação de Pagador."),
            new Ocorrencia("YF", "Alegação já Informada", "Ocorrência especifica para o tipo de serviço alegação de Pagador.\r\nObservação: As ocorrências iniciadas com 'ZA' tem caráter informativo para o cliente"),
            new Ocorrencia("ZA", "Agência / Conta do Favorecido Substituída", "Beneficiário é correntista do banco favorecido, mas seu numero de agencia e conta foram alterados.\r\nNota: Pode ter mudado sua conta de agencia ou os dados podem ter sofrido algum tipo de atualização/alteração."),
            new Ocorrencia("ZB", "Divergência entre o primeiro e último nome do beneficiário versus primeiro e último nome na Receita Federal"),
            new Ocorrencia("ZC", "Confirmação de Antecipação de Valor", "Verificar o cadastro do beneficiário junto a receita federal para identificar se existe alguma divergência de informações."),
            new Ocorrencia("ZD", "Antecipação parcial de valor"),
            new Ocorrencia("ZE", "Título bloqueado na base", "Titulo Bloqueado ou não encontrado na Base da CIP"),
            new Ocorrencia("ZF", "Sistema em contingência – título valor maior que referência"),
            new Ocorrencia("ZG", "Sistema em contingência – título vencido"),
            new Ocorrencia("ZH", "Sistema em contingência – título indexado"),
            new Ocorrencia("ZI", "Beneficiário divergente"),
            new Ocorrencia("ZJ", "Limite de pagamentos parciais excedido"),
            new Ocorrencia("ZK", "Boleto já liquidado"),
            new Ocorrencia("PA", "Pix não efetivado"),
            new Ocorrencia("PB", "Transação interrompida devido a erro no PSP do Recebedor"),
            new Ocorrencia("PC", "Número da conta transacional encerrada no PSP do Recebedor"),
            new Ocorrencia("PD", "Tipo incorreto para a conta transacional especificada"),
            new Ocorrencia("PE", "Tipo de transação não é suportado/autorizado na conta transacional especificada"),
            new Ocorrencia("PF", "CPF/CNPJ do usuário recebedor não é consistente com o titular da conta transacional especificada"),
            new Ocorrencia("PG", "CPF/CNPJ do usuário recebedor incorreto"),
            new Ocorrencia("PH", "Ordem rejeitada pelo PSP do Recebedor"),
            new Ocorrencia("PI", "ISPB do PSP do Pagador inválido ou inexistente"),
            new Ocorrencia("PJ", "Chave não cadastrada no DICT"),
            new Ocorrencia("PK", "QR Code inválido/vencido"),
            new Ocorrencia("PL", "Forma de iniciação inválida"),
            new Ocorrencia("PM", "Chave de pagamento inválida"),
            new Ocorrencia("PN", "Chave de pagamento não informada"),
            new Ocorrencia("5A", "Agendado sob lista de debito", "Pagamento agendado que faz parte de uma lista com um número para autorização."),
            new Ocorrencia("5B", "Pagamento não autoriza sob lista de debito", "Pagamento da lista não foi autorizado"),
            new Ocorrencia("5C", "Pagamento não autoriza sob lista de debito", "Lista de pagamento não permite mais de uma modalidade"),
            new Ocorrencia("5D", "Lista com mais de uma data de pagamento", "Lista de pagamento não permite mais de uma data de pagamento"),
            new Ocorrencia("5E", "Número de lista duplicado", "Número da lista enviado pelo cliente já foi utilizado"),
            new Ocorrencia("5F", "Lista de debito vencida e não autorizada", "Pagamentos que pertence a uma determinada lista estão vencidos e não foram autorizados"),
            new Ocorrencia("5I", "Ordem de Pagamento emitida", "Pagamento realizado ao favorecido nesta data"),
            new Ocorrencia("5J", "Ordem de pagamento com data limite vencida"),
            new Ocorrencia("5M", "Número de lista de debito invalida", "Número de lista inválido (deve ser numérico)"),
            new Ocorrencia("5T", "Pagamento realizado em contrato na condição de TESTE")
        };
    }
}

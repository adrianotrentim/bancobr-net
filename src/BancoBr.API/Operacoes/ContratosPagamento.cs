using System;
using System.Collections.Generic;
using BancoBr.Common.Instances;

namespace BancoBr.API.Operacoes
{
    /// <summary>Ciclo de execução do pagamento. Não representa assinatura/autorização.</summary>
    public enum SituacaoPagamento
    {
        Desconhecida = 0,
        EmProcessamento = 1,
        Agendado = 2,
        Efetivado = 3,
        Cancelado = 4,
        Rejeitado = 5,
        Devolvido = 6
    }

    /// <summary>Autorização informada pelo banco. NãoInformada evita presumir assinaturas ou aprovações ausentes do retorno.</summary>
    public enum SituacaoAutorizacao
    {
        NaoInformada = 0,
        AguardandoAutorizacao = 1,
        ParcialmenteAutorizado = 2,
        Autorizado = 3,
        Recusado = 4,
        Expirado = 5
    }

    /// <summary>Modalidade necessária para reconstruir uma consulta sem expor DTOs próprios dos bancos.</summary>
    public enum ModalidadePagamento { Boleto = 1, Convenio = 2, Ted = 3, Pix = 4, PixQrCode = 5 }

    /// <summary>Pagamento com identidade persistida pelo consumidor antes do envio. Reutilize o mesmo IdOperacao nas tentativas.</summary>
    public sealed class RequisicaoPagamento
    {
        public Guid IdOperacao { get; set; }
        public Movimento Movimento { get; set; }
        /// <summary>Unidade do perfil de arrecadação, quando exigida pelo contrato da conta.</summary>
        public int Unidade { get; set; }
    }

    /// <summary>Referência persistível com Newtonsoft.Json. Trate-a como opaca e reutilize na mesma conta/banco, inclusive após reiniciar o processo.</summary>
    public sealed class ReferenciaPagamento
    {
        public Guid IdOperacao { get; set; }
        public int Banco { get; set; }
        public string ContaOrigem { get; set; }
        public ModalidadePagamento Modalidade { get; set; }
        public string IdentificadorBanco { get; set; }
        /// <summary>Identidade opaca para recuperar uma solicitação aceita sem identificador bancário na resposta.</summary>
        public string IdentidadeConsulta { get; set; }
        public long IdAgendamento { get; set; }
        public long? Nsu { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime DataPagamento { get; set; }
        public decimal ValorPagamento { get; set; }
        public int Unidade { get; set; }
    }

    /// <summary>Resposta original de um recurso bancário para diagnóstico opcional. Pode conter dados bancários; não contém token ou headers.</summary>
    public sealed class RetornoBancario
    {
        public int HttpStatusCode { get; internal set; }
        public string Corpo { get; internal set; }
    }

    /// <summary>Retorno detalhado comum a todos os bancos. Os estados antigos de Movimento permanecem inalterados.</summary>
    public sealed class ResultadoPagamento
    {
        public Guid IdOperacao { get; internal set; }
        public ReferenciaPagamento Referencia { get; internal set; }
        public Movimento Movimento { get; internal set; }
        public SituacaoPagamento Situacao { get; internal set; }
        public SituacaoAutorizacao Autorizacao { get; internal set; }
        public string SituacaoOriginal { get; internal set; }
        public string Motivo { get; internal set; }
        /// <summary>Todas as respostas das etapas de consulta, iniciação e confirmação, em ordem.</summary>
        public IReadOnlyList<RetornoBancario> RetornosBanco { get; internal set; }
    }

    /// <summary>Comprovante comum: conteúdo binário, link temporário ou dados de autenticação, conforme disponibilizado pelo banco.</summary>
    public sealed class ResultadoComprovante
    {
        public ResultadoPagamento Pagamento { get; internal set; }
        public byte[] Conteudo { get; internal set; }
        public string Link { get; internal set; }
        public DateTime? ValidadeLink { get; internal set; }
        public string Autenticacao { get; internal set; }
    }
}

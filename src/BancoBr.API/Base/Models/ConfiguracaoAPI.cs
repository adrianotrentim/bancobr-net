using BancoBr.API.Core.Http;
using BancoBr.Common.Instances;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Credenciais, certificado e conta (<see cref="Correntista"/>) de acesso à API do banco, informadas em <c>BancoApi.Conectar</c>. <see cref="IdWorkspace"/>
    /// só se aplica ao Santander.
    /// </summary>
    public class ConfiguracaoAPI
    {
        public CertificateSource CertificadoDigital { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string IdWorkspace { get; set; }

        /// <summary>Chave da aplicação no Portal Developers BB (gw-dev-app-key).</summary>
        public string ChaveAplicacao { get; set; }

        /// <summary>Número do contrato de pagamentos do Banco do Brasil.</summary>
        public int? NumeroContratoPagamento { get; set; }

        /// <summary>Seleciona os endpoints de homologação do Banco do Brasil.</summary>
        public bool AmbienteHomologacao { get; set; }

        /// <summary>
        /// Conta pagadora/titular das operações: usada como origem em todos os métodos de
        /// <c>BancoApi</c> (pagamento, consulta, cancelamento, DDA, extrato, workspace). Obrigatório:
        /// <see cref="Correntista.Banco"/> (código de compensação) define qual banco é conectado.
        /// </summary>
        public Correntista Correntista { get; set; }
    }
}

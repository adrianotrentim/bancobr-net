using BancoBr.API.Core.Http;

namespace BancoBr.API.Base.Models
{
    /// <summary>
    /// Credenciais de acesso à API do banco, informadas em <c>BancoApi.Conectar</c>. <see cref="IdWorkspace"/>
    /// só se aplica ao Santander.
    /// </summary>
    public class ConfiguracaoAPI
    {
        public CertificateSource CertificadoDigital { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string IdWorkspace { get; set; }

    }
}

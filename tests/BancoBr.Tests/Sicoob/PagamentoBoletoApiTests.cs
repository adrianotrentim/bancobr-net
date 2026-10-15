using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BancoBr.API;
using BancoBr.API.Base.Models;
using BancoBr.API.Core.Http;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using Xunit;

namespace BancoBr.Tests.Sicoob
{
    public class PagamentoBoletoApiTests
    {
        private static CertificateSource CriarCertificateSourceFake()
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=fake", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificado = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1));
            var pfxBytes = certificado.Export(X509ContentType.Pfx, "senha");
            return CertificateSource.FromPfxBytes(pfxBytes, "senha");
        }

        [Fact]
        public void Conectar_BancoSicoob_RetornaBancoApi()
        {
            var resultado = BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "fake-client-id", CertificadoDigital = CriarCertificateSourceFake(), Correntista = new Correntista { Banco = (int)BancoEnum.Sicoob } }, new FakeOAuthTokenProvider());

            Assert.NotNull(resultado);
        }

        [Fact]
        public void Conectar_SemCorrentista_LancaArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() => BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "fake-client-id", CertificadoDigital = CriarCertificateSourceFake() }, new FakeOAuthTokenProvider()));
        }

        [Fact]
        public void Conectar_BancoNaoImplementado_LancaException()
        {
            Assert.Throws<System.Exception>(() => BancoApi.Conectar(new ConfiguracaoAPI { ClientId = "fake-client-id", CertificadoDigital = CriarCertificateSourceFake(), Correntista = new Correntista { Banco = (int)BancoEnum.BradescoSA } }, new FakeOAuthTokenProvider()));
        }
    }
}

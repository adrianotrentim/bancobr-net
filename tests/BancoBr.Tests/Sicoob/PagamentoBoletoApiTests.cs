using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BancoBr.API;
using BancoBr.API.Core.Http;
using BancoBr.Common.Enums;
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
            var resultado = BancoApi.Conectar(BancoEnum.Sicoob, "fake-client-id", CriarCertificateSourceFake(), new FakeOAuthTokenProvider());

            Assert.NotNull(resultado);
        }

        [Fact]
        public void Conectar_BancoNaoImplementado_LancaException()
        {
            Assert.Throws<System.Exception>(() => BancoApi.Conectar(BancoEnum.BradescoSA, "fake-client-id", CriarCertificateSourceFake(), new FakeOAuthTokenProvider()));
        }
    }
}

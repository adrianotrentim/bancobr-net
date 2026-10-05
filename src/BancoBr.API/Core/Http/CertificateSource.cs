using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace BancoBr.API.Core.Http
{
    /// <summary>
    /// Resolve o certificado digital X.509 (ICP-Brasil A1) usado para autenticação mTLS
    /// exigida por algumas APIs bancárias (ex.: Sicoob).
    /// </summary>
    /// <remarks>
    /// PEM (cert + chave separados) não é suportado: a importação de chave privada PEM
    /// (RSA.ImportFromPem/X509Certificate2.CreateFromPem) só existe a partir do .NET Core 3.0+
    /// e este projeto é netstandard2.0. Use PFX ou o certificate store do Windows.
    /// </remarks>
    public class CertificateSource
    {
        private readonly X509Certificate2 _certificate;

        private CertificateSource(X509Certificate2 certificate)
        {
            _certificate = certificate;
        }

        public X509Certificate2 GetCertificate() => _certificate;

        public static CertificateSource FromPfxFile(string pfxPath, string password)
        {
            return new CertificateSource(new X509Certificate2(pfxPath, password, X509KeyStorageFlags.Exportable));
        }

        public static CertificateSource FromPfxBytes(byte[] pfxBytes, string password)
        {
            return new CertificateSource(new X509Certificate2(pfxBytes, password, X509KeyStorageFlags.Exportable));
        }

        public static CertificateSource FromStore(string thumbprint, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                var found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                if (found.Count == 0)
                {
                    throw new InvalidOperationException($"Certificado com thumbprint '{thumbprint}' não encontrado em {storeLocation}/{storeName}.");
                }

                return new CertificateSource(found[0]);
            }
        }
    }
}

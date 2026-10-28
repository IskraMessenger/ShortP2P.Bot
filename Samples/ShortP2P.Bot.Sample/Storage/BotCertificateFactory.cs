using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ShortP2P.Bot.Sample.Storage;

internal static class BotCertificateFactory
{
    public static BotCertificateRecord Create(string subjectCommonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={SanitizeCommonName(subjectCommonName)}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") },
            true));

        using var generated = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        var pfx = ExportPfx(generated, rsa);
        using var certificate = new X509Certificate2(pfx, (string?)null, X509KeyStorageFlags.Exportable);
        var fingerprint = Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant();
        if (fingerprint.Length != 64)
            throw new InvalidOperationException("Generated certificate fingerprint is invalid.");

        return new BotCertificateRecord
        {
            Pfx = pfx,
            FingerprintSha256 = fingerprint
        };
    }

    private static byte[] ExportPfx(X509Certificate2 generated, RSA rsa)
    {
        try
        {
            return generated.Export(X509ContentType.Pfx);
        }
        catch (CryptographicException)
        {
            using var withKey = generated.CopyWithPrivateKey(rsa);
            return withKey.Export(X509ContentType.Pfx);
        }
    }

    private static string SanitizeCommonName(string subjectCommonName)
    {
        var trimmed = (subjectCommonName ?? "").Trim();
        var cleaned = new string(trimmed.Where(c => c is not ',' and not '+' and not '"' and not '\\' and not '<' and not '>' and not ';').ToArray());
        if (cleaned.Length == 0)
            cleaned = "sample-bot";
        if (cleaned.Length > 64)
            cleaned = cleaned[..64];
        return cleaned;
    }
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShortP2P.Bot.Security;

namespace ShortP2P.Bot.Tests;

public sealed class BotCertificateProviderTests
{
    [Fact]
    public void AutoGenerates_AndReuses_Pfx()
    {
        var path = Path.Combine(Path.GetTempPath(), "ShortP2P.Bot.Tests", Guid.NewGuid().ToString("N"), "auto.pfx");
        try
        {
            var options = Options.Create(new BotCertificateOptions { AutoGeneratePfxPath = path });
            using var first = new BotCertificateProvider(options, NullLogger<BotCertificateProvider>.Instance);
            var fingerprint1 = first.FingerprintSha256;
            Assert.True(CertificateFingerprintRules.IsValid(fingerprint1));
            Assert.True(File.Exists(path));

            using var second = new BotCertificateProvider(options, NullLogger<BotCertificateProvider>.Instance);
            Assert.Equal(fingerprint1, second.FingerprintSha256);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Loads_ConfiguredPfx()
    {
        var path = Path.Combine(Path.GetTempPath(), "ShortP2P.Bot.Tests", Guid.NewGuid().ToString("N"), "custom.pfx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=Custom.Bot",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            using var created = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddYears(1));
            File.WriteAllBytes(path, created.Export(X509ContentType.Pfx, "secret"));

            var expected = BotCertificateProvider.ComputeFingerprintSha256(created);

            var options = Options.Create(new BotCertificateOptions
            {
                PfxPath = path,
                PfxPassword = "secret"
            });

            using var provider = new BotCertificateProvider(options, NullLogger<BotCertificateProvider>.Instance);
            Assert.Equal(expected, provider.FingerprintSha256);
            using var loaded = provider.GetCertificate();
            Assert.Contains("Custom.Bot", loaded.Subject, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Normalize_StripsSeparators_AndLowercases()
    {
        var withSeparators = "AA:BB:CC:DD-" + new string('0', 56);
        var normalized = CertificateFingerprintRules.Normalize(withSeparators);
        Assert.Equal("aabbccdd" + new string('0', 56), normalized);
        Assert.True(CertificateFingerprintRules.AreEqual(withSeparators, normalized));
        Assert.True(CertificateFingerprintRules.IsValid(normalized));
    }
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ShortP2P.Bot.Security;

/// <summary>
/// Loads a configured PFX or creates a reusable self-signed certificate for the bot.
/// </summary>
public sealed class BotCertificateProvider : IBotCertificateProvider, IDisposable
{
    private const string DefaultFileName = "shortp2p-bot-certificate.pfx";
    private static readonly TimeSpan SelfSignedValidity = TimeSpan.FromDays(365 * 5);

    private readonly BotCertificateOptions _options;
    private readonly ILogger<BotCertificateProvider> _logger;
    private readonly object _sync = new();
    private X509Certificate2? _certificate;
    private string? _fingerprintSha256;
    private bool _disposed;

    public BotCertificateProvider(
        IOptions<BotCertificateOptions> options,
        ILogger<BotCertificateProvider> logger)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string FingerprintSha256
    {
        get
        {
            EnsureLoaded();
            return _fingerprintSha256!;
        }
    }

    public X509Certificate2 GetCertificate()
    {
        EnsureLoaded();
        return new X509Certificate2(_certificate!);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_sync)
        {
            if (_disposed)
                return;

            _certificate?.Dispose();
            _certificate = null;
            _disposed = true;
        }
    }

    private void EnsureLoaded()
    {
        if (_certificate is not null)
            return;

        lock (_sync)
        {
            if (_certificate is not null)
                return;

            if (_disposed)
                throw new ObjectDisposedException(nameof(BotCertificateProvider));

            _certificate = LoadOrCreateCertificate();
            _fingerprintSha256 = ComputeFingerprintSha256(_certificate);

            _logger.LogInformation(
                "Bot certificate ready: subject={Subject}, fingerprint={Fingerprint}",
                _certificate.Subject,
                _fingerprintSha256);
        }
    }

    private X509Certificate2 LoadOrCreateCertificate()
    {
        var configuredPath = _options.PfxPath?.Trim();
        if (!string.IsNullOrEmpty(configuredPath))
        {
            if (!File.Exists(configuredPath))
            {
                throw new FileNotFoundException(
                    $"Configured bot certificate PFX was not found: '{configuredPath}'.",
                    configuredPath);
            }

            _logger.LogDebug("Loading bot certificate from configured PFX {Path}", configuredPath);
            return LoadPfx(configuredPath, _options.PfxPassword);
        }

        var autoPath = ResolveAutoGeneratePath();
        if (File.Exists(autoPath))
        {
            _logger.LogDebug("Reusing auto-generated bot certificate at {Path}", autoPath);
            return LoadPfx(autoPath, _options.PfxPassword);
        }

        _logger.LogInformation("Generating self-signed bot certificate at {Path}", autoPath);
        return CreateAndPersistSelfSigned(autoPath, _options.PfxPassword);
    }

    private string ResolveAutoGeneratePath()
    {
        var configured = _options.AutoGeneratePfxPath?.Trim();
        if (!string.IsNullOrEmpty(configured))
            return Path.GetFullPath(configured);

        return Path.Combine(AppContext.BaseDirectory, DefaultFileName);
    }

    private static X509Certificate2 LoadPfx(string path, string? password)
    {
        var bytes = File.ReadAllBytes(path);
        return new X509Certificate2(
            bytes,
            password ?? string.Empty,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    private static X509Certificate2 CreateAndPersistSelfSigned(string path, string? password)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=ShortP2P.Bot",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.Add(SelfSignedValidity);
        using var created = request.CreateSelfSigned(notBefore, notAfter);

        var exportPassword = password ?? string.Empty;
        var pfxBytes = created.Export(X509ContentType.Pfx, exportPassword);
        File.WriteAllBytes(path, pfxBytes);

        return new X509Certificate2(
            pfxBytes,
            exportPassword,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    internal static string ComputeFingerprintSha256(X509Certificate2 certificate)
    {
        if (certificate is null) throw new ArgumentNullException(nameof(certificate));

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(certificate.RawData);
        return CertificateFingerprintRules.Normalize(ToHexLower(hash));
    }

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = GetHexChar(b >> 4);
            chars[i * 2 + 1] = GetHexChar(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexChar(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}

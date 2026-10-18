using System.Security.Cryptography.X509Certificates;

namespace ShortP2P.Bot.Security;

/// <summary>
/// Provides the bot TLS certificate used for registration/login fingerprints
/// (and optionally as a client certificate).
/// </summary>
public interface IBotCertificateProvider
{
    /// <summary>Normalized SHA-256 fingerprint (64 lowercase hex chars, no separators).</summary>
    string FingerprintSha256 { get; }

    /// <summary>Loads (or generates) the bot certificate. Caller must dispose.</summary>
    X509Certificate2 GetCertificate();
}

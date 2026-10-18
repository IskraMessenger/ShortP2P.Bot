namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Server TLS certificate info for client pinning.</summary>
public sealed class ServerCertificateResponse
{
    /// <summary>SHA-256 fingerprint, typically hex (lowercase, no separators) or colon-separated.</summary>
    public string FingerprintSha256 { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public DateTime? NotAfterUtc { get; set; }
}

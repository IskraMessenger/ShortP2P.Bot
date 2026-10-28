namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>SHA-256 fingerprint of this messenger server TLS certificate (hex).</summary>
    public string ServerFingerprintSha256 { get; set; } = string.Empty;
}

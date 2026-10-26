namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotRegisterRequest
{
    public string NetworkId { get; set; } = string.Empty;
    public string BotName { get; set; } = string.Empty;
    public string BotReadableName { get; set; } = string.Empty;
    public string BotDescription { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the bot TLS certificate (hex, optional separators).
    /// The messenger server requires it on registration and checks it again on login.
    /// </summary>
    public string? FingerprintSha256 { get; set; }

    /// <summary>
    /// RSA public key JSON (<c>{"M":"...","E":"..."}</c>) for client → bot payload encryption.
    /// Required by current messenger servers; exposed via GET /bots.
    /// </summary>
    public string MessagePublicKey { get; set; } = string.Empty;
}

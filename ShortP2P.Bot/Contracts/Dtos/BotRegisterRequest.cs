namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>New bot registration payload.</summary>
public sealed class BotRegisterRequest
{
    /// <summary>Short network id (base64url, ~16 chars).</summary>
    public string NetworkId { get; set; } = string.Empty;

    /// <summary>Bot logical name (max <see cref="BotLimits.MaxBotNameLength"/> chars).</summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>Human-readable display name (max <see cref="BotLimits.MaxBotReadableNameLength"/> chars).</summary>
    public string BotReadableName { get; set; } = string.Empty;

    /// <summary>Bot description (max <see cref="BotLimits.MaxBotDescriptionLength"/> chars).</summary>
    public string BotDescription { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the bot TLS certificate (hex; optional <c>:</c>/<c>-</c>/space separators).
    /// Stored by the server and checked on later <see cref="BotLoginRequest"/>.
    /// </summary>
    public string FingerprintSha256 { get; set; } = string.Empty;

    /// <summary>
    /// RSA public key JSON (<c>{"M":"...","E":"..."}</c>) for client → bot payload encryption.
    /// Required by current messenger servers; exposed via GET /bots.
    /// </summary>
    public string MessagePublicKey { get; set; } = string.Empty;
}

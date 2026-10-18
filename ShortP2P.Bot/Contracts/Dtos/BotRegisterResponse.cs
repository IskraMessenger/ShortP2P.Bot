namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Successful bot registration result.</summary>
public sealed class BotRegisterResponse
{
    /// <summary>Short network id (base64url, ~16 chars).</summary>
    public string NetworkId { get; set; } = string.Empty;

    /// <summary>Bot logical name.</summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string BotReadableName { get; set; } = string.Empty;

    /// <summary>Bot description.</summary>
    public string BotDescription { get; set; } = string.Empty;

    /// <summary>
    /// Server-generated bot key: base64, exactly <see cref="BotLimits.BotKeyLength"/> characters.
    /// Returned once on registration; required later for <see cref="BotLoginRequest"/>.
    /// <para><b>Secret:</b> known only to the server and the bot; must not be logged, relayed to clients, or shared otherwise.</para>
    /// </summary>
    public string BotKey { get; set; } = string.Empty;

    /// <summary>SHA-256 fingerprint of this messenger server TLS certificate (hex).</summary>
    public string ServerFingerprintSha256 { get; set; } = string.Empty;
}

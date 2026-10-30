namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Bot authorization payload.</summary>
public sealed class BotLoginRequest
{
    /// <summary>Short network id (base64url, ~16 chars).</summary>
    public string NetworkId { get; set; } = string.Empty;

    /// <summary>Bot logical name (max <see cref="BotLimits.MaxBotNameLength"/> chars).</summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>
    /// Bot key: base64, exactly <see cref="BotLimits.BotKeyLength"/> characters.
    /// Issued by the server on registration; known only to the server and the bot.
    /// <para><b>Secret:</b> must not be logged, relayed to clients, or shared otherwise.</para>
    /// </summary>
    public string BotKey { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the bot TLS certificate (hex; optional <c>:</c>/<c>-</c>/space separators).
    /// Must match the fingerprint stored at registration.
    /// </summary>
    public string FingerprintSha256 { get; set; } = string.Empty;

    /// <summary>
    /// Current bot software version. Zero or greater.
    /// Replaces the version stored at registration.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Commands from <c>bot_commands</c>. <c>BotMessenger</c> fills this from the file before the request is sent.
    /// </summary>
    public List<BotCommandDto> Commands { get; set; } = new();
}

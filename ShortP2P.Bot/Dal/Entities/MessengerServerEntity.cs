namespace ShortP2P.Bot.Dal.Entities;

/// <summary>
/// Messenger server entry in the bot's local RDBMS pool.
/// Populated after a successful bot registration on that server.
/// </summary>
public sealed class MessengerServerEntity
{
    public long Id { get; set; }

    /// <summary>Absolute base URL, e.g. https://messenger.example.com:7196 (no trailing slash).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Bot network id registered on this server.</summary>
    public string NetworkId { get; set; } = string.Empty;

    /// <summary>Bot logical name registered on this server (needed for login).</summary>
    public string BotName { get; set; } = string.Empty;

    /// <summary>Secret bot key issued by this server. Do not log.</summary>
    public string BotKey { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the messenger server TLS certificate, pinned at registration.
    /// Normalized lowercase hex without separators.
    /// </summary>
    public string ServerFingerprintSha256 { get; set; } = string.Empty;

    /// <summary>
    /// True when the server certificate fingerprint no longer matches the pin from registration.
    /// Blocked servers are excluded from messaging and health-check recovery until re-registered.
    /// </summary>
    public bool IsBlocked { get; set; }

    /// <summary>False when the server was marked unreachable (transport / health failure).</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>UTC of the last successful send or receive via this server.</summary>
    public DateTime? LastSuccessfulAtUtc { get; set; }

    public DateTime RegisteredAtUtc { get; set; }

    public DateTime? LastHealthCheckAtUtc { get; set; }

    /// <summary>Optional last error message from health check, failed transport, or cert block.</summary>
    public string? LastError { get; set; }
}

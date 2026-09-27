namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>
/// Encrypted message between a client and a bot.
/// <see cref="NetworkId"/> is the client; server does not decrypt the payload.
/// </summary>
public sealed class BotClientMessageDto
{
    public string NetworkId { get; set; } = string.Empty;
    public string EncryptedMessageBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Optional end-to-end correlation id (client ↔ bot); max <see cref="BotLimits.MaxCorrelationIdLength"/> chars;
    /// relayed unchanged by the server.
    /// </summary>
    public string? CorrelationId { get; set; }
}

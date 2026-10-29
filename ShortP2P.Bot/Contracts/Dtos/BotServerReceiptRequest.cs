namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Bot confirms it received a client → bot message. The server then drops the message and its chunks.</summary>
public sealed class BotServerReceiptRequest
{
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Bot short network id (base64url, ~16 chars).</summary>
    public string BotNetworkId { get; set; } = string.Empty;

    /// <summary>
    /// Bot key: base64, exactly <see cref="BotLimits.BotKeyLength"/> characters.
    /// <para><b>Secret:</b> known only to the server and the bot.</para>
    /// </summary>
    public string BotKey { get; set; } = string.Empty;

    public string MessageId { get; set; } = string.Empty;
}

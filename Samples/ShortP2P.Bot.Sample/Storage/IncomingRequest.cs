namespace ShortP2P.Bot.Sample.Storage;

internal sealed class IncomingRequest
{
    public DateTime ReceivedAtUtc { get; init; }

    public string ClientNetworkId { get; init; } = "";

    public string? CorrelationId { get; init; }

    public string MessageText { get; init; } = "";

    public string EncryptedMessageBase64 { get; init; } = "";

    public string ReplyText { get; init; } = "";
}

namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotSendMessagesRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string BotNetworkId { get; set; } = string.Empty;
    public string BotKey { get; set; } = string.Empty;
    public IReadOnlyList<BotClientMessageDto> Messages { get; set; } = Array.Empty<BotClientMessageDto>();
}

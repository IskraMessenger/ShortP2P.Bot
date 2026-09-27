namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotWaitForIncomeMessagesResponse
{
    public string RequestId { get; set; } = string.Empty;
    public IReadOnlyList<BotClientMessageDto> Messages { get; set; } = Array.Empty<BotClientMessageDto>();
}

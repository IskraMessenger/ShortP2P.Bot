namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotWaitForIncomeMessagesRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string BotNetworkId { get; set; } = string.Empty;
    public string BotKey { get; set; } = string.Empty;
    public int? TimeoutSeconds { get; set; }
}

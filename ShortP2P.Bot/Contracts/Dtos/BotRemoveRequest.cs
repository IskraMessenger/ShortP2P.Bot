namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotRemoveRequest
{
    public string NetworkId { get; set; } = string.Empty;
    public string BotKey { get; set; } = string.Empty;
}

namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotRegisterRequest
{
    public string NetworkId { get; set; } = string.Empty;
    public string BotName { get; set; } = string.Empty;
    public string BotReadableName { get; set; } = string.Empty;
    public string BotDescription { get; set; } = string.Empty;
}

namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotRegisterResponse
{
    public string NetworkId { get; set; } = string.Empty;
    public string BotName { get; set; } = string.Empty;
    public string BotReadableName { get; set; } = string.Empty;
    public string BotDescription { get; set; } = string.Empty;

    /// <summary>Server-generated secret bot key (base64). Do not log or share.</summary>
    public string BotKey { get; set; } = string.Empty;
}

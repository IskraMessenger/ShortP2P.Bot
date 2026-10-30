namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Bot network id and the commands loaded from <c>bot_commands</c>.</summary>
public sealed class BotCommandsDto
{
    public string BotNetworkId { get; set; } = "";

    public IReadOnlyList<BotCommandDto> Commands { get; set; } = new List<BotCommandDto>();
}

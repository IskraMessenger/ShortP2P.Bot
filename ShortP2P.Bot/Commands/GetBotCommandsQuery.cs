namespace ShortP2P.Bot.Commands;

/// <summary>Asks a bot for the commands loaded from its <c>bot_commands</c> file.</summary>
public sealed class GetBotCommandsQuery
{
    public GetBotCommandsQuery(string botNetworkId)
    {
        BotNetworkId = botNetworkId;
    }

    public string BotNetworkId { get; }
}

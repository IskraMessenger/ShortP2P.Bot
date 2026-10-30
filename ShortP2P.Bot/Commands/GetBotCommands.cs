using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Commands;

/// <summary>Returns the bot network id and the commands from <c>bot_commands</c>.</summary>
public sealed class GetBotCommands
{
    private readonly BotCommandCatalog _catalog;

    public GetBotCommands(BotCommandCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public BotCommandsDto Execute(GetBotCommandsQuery query)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));

        var networkId = (query.BotNetworkId ?? "").Trim();
        if (networkId.Length == 0)
            throw new ArgumentException("Bot network id is required.", nameof(query));

        return new BotCommandsDto
        {
            BotNetworkId = networkId,
            Commands = _catalog.Commands.ToArray()
        };
    }

    /// <summary>
    /// Plain text is left for the bot's other handlers.
    /// A command is either accepted or refused with <see cref="BotCommandRejection.Code"/>.
    /// </summary>
    public BotCommandInspection Inspect(string? text) => _catalog.Inspect(text);
}

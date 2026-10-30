namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>
/// One bot command. <see cref="Args"/> is null or empty when the command takes no arguments.
/// </summary>
public sealed class BotCommandDto
{
    /// <summary>Command name, including the leading slash. Example: <c>/start</c>.</summary>
    public string Command { get; set; } = "";

    /// <summary>
    /// Positional arguments. Null or empty when the command takes none.
    /// Each item is an internal name plus <c>dynamic</c>, <c>string</c>, <c>number</c>, or <c>datetime</c>.
    /// </summary>
    public List<BotCommandArgumentDto>? Args { get; set; }

    /// <summary>Short description shown to clients.</summary>
    public string Description { get; set; } = "";
}

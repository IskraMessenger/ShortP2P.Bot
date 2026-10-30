namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>
/// One positional argument. <see cref="ArgName"/> is an internal label.
/// <see cref="ArgType"/> is <c>dynamic</c>, <c>string</c>, <c>number</c>, or <c>datetime</c>.
/// </summary>
public sealed class BotCommandArgumentDto
{
    public string ArgName { get; set; } = "";

    public string ArgType { get; set; } = "";
}

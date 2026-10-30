namespace ShortP2P.Bot.Commands;

/// <summary>Result of deciding whether incoming text is a command and whether that command is valid.</summary>
public sealed class BotCommandInspection
{
    /// <summary>True when the text matches the command pattern <c>/Name</c>.</summary>
    public bool IsCommand { get; set; }

    /// <summary>True when <see cref="IsCommand"/> is true and the command matches <c>bot_commands</c>.</summary>
    public bool IsAccepted { get; set; }

    /// <summary><see cref="BotCommandRejection.Code"/> when a command was refused. Null otherwise.</summary>
    public string? RejectionCode { get; set; }

    /// <summary>Text the bot sends to the client when a command is refused. Null otherwise.</summary>
    public string? RejectionMessage { get; set; }
}

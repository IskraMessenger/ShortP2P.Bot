namespace ShortP2P.Bot.Commands;

/// <summary>Refusal the bot sends through the messenger server when a command cannot be handled.</summary>
public static class BotCommandRejection
{
    /// <summary>Stable refusal code relayed to the client inside the reply text.</summary>
    public const string Code = "BOT_COMMAND_REJECTED";

    public static string Message(string detail) => Code + ": " + detail;
}

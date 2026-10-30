namespace ShortP2P.Bot;

/// <summary>Host settings sent to messenger servers with the bot.</summary>
public sealed class BotHostOptions
{
    public const string SectionName = "ShortP2PBot";

    /// <summary>
    /// Software version reported on register and login when the request does not set one.
    /// Zero or greater.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Path to the JSON command list. Relative paths are resolved from the application base directory.
    /// Default file name is <c>bot_commands</c>.
    /// </summary>
    public string CommandsFile { get; set; } = "bot_commands";

    /// <summary>
    /// When true, register, login and message checks fail if <c>bot_commands</c> is missing or invalid.
    /// </summary>
    public bool RequireCommandsFile { get; set; } = true;
}

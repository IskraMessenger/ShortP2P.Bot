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
}

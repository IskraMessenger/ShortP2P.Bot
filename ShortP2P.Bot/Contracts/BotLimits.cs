namespace ShortP2P.Bot.Contracts;

/// <summary>Field length limits for bot API payloads.</summary>
public static class BotLimits
{
    public const int MaxBotNameLength = 256;
    public const int MaxBotReadableNameLength = 100;
    public const int MaxBotDescriptionLength = 500;

    /// <summary>Exact length of bot key (base64).</summary>
    public const int BotKeyLength = 64;
}

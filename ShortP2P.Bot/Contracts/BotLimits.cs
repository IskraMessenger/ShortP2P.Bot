namespace ShortP2P.Bot.Contracts;

/// <summary>Field length limits for bot registration / auth API payloads.</summary>
public static class BotLimits
{
    public const int MaxBotNameLength = 256;

    public const int MaxBotReadableNameLength = 100;

    public const int MaxBotDescriptionLength = 500;

    /// <summary>
    /// Exact length of bot key (base64). Must match the server <c>BotKeyGenerator.KeyBase64Length</c>.
    /// <para><b>Secret:</b> shared only between the server and the bot; never expose to clients or third parties.</para>
    /// </summary>
    public const int BotKeyLength = 64;

    /// <summary>Max length of optional end-to-end <c>correlationId</c> on client ↔ bot messages.</summary>
    public const int MaxCorrelationIdLength = 32;
}

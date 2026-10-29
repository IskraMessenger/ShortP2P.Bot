namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Standard messenger-server API error body.</summary>
public sealed class ApiError
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>Server rejected a bot chunk upload because free-disk budget cannot hold <c>totalBytes</c> (HTTP 507).</summary>
    public const string InsufficientStorage = "InsufficientStorage";
}

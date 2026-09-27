namespace ShortP2P.Bot.Contracts.Dtos;

public sealed class BotLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

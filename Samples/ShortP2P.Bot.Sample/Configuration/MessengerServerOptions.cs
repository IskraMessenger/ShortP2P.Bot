namespace ShortP2P.Bot.Sample.Configuration;

public sealed class MessengerServerOptions
{
    public string BaseUrl { get; set; } = "";

    public int? LongPollTimeoutSeconds { get; set; }
}
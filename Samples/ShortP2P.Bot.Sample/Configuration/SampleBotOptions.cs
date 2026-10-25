namespace ShortP2P.Bot.Sample.Configuration;

public sealed class SampleBotOptions
{
    public MessengerServerOptions MessengerServer { get; set; } = new();

    public BotOptions Bot { get; set; } = new();

    public StorageOptions Storage { get; set; } = new();
}
namespace ShortP2P.Bot.Sample.Configuration;

public sealed class BotOptions
{
    public string NetworkId { get; set; } = "";

    public string Name { get; set; } = "";

    public string ReadableName { get; set; } = "";

    public string Description { get; set; } = "";

    /// <summary>Integer software version reported to messenger servers.</summary>
    public int Version { get; set; } = 1;

    /// <summary>When true, the meteo bot replies with fixed sample weather and does not call weather or geocoding APIs.</summary>
    public bool IsMock { get; set; }
}

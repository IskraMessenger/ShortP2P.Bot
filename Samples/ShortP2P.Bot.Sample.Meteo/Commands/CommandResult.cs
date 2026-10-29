using ShortP2P.Bot.Sample.Meteo.Weather;

namespace ShortP2P.Bot.Sample.Meteo.Commands;

internal sealed class CommandResult
{
    public required string Text { get; init; }

    public string Language { get; init; } = MeteoTexts.DefaultLanguage;

    /// <summary>Set only when a weather report was produced. Media is built from this report.</summary>
    public WeatherReport? Report { get; init; }

    public static CommandResult TextOnly(string text, string language) =>
        new() { Text = text, Language = language };
}

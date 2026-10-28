namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal sealed class WeatherReport
{
    public required string DisplayName { get; init; }

    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public required double TemperatureC { get; init; }

    public required int HumidityPercent { get; init; }

    public required int WeatherCode { get; init; }

    public required string ObservedAt { get; init; }

    public required DateTime FetchedAtUtc { get; init; }
}

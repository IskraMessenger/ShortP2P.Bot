using System.Globalization;

namespace ShortP2P.Bot.Sample.Meteo.Weather;

/// <summary>Fixed weather used when <c>Bot:IsMock</c> is true.</summary>
internal static class MockWeather
{
    public const double TemperatureC = 8.2;
    public const int HumidityPercent = 70;
    public const int WeatherCode = 3;
    public const string ObservedAt = "2026-10-29 12:00";

    public static WeatherReport ForPlace(string query, DateTime utcNow)
    {
        var name = (query ?? "").Trim();
        if (name.Length == 0)
            name = "mock";
        return Create(name, latitude: 0, longitude: 0, utcNow);
    }

    public static WeatherReport ForCoordinates(GeoPoint point, DateTime utcNow)
    {
        var name = point.Latitude.ToString("0.####", CultureInfo.InvariantCulture)
            + ", "
            + point.Longitude.ToString("0.####", CultureInfo.InvariantCulture);
        return Create(name, point.Latitude, point.Longitude, utcNow);
    }

    private static WeatherReport Create(string displayName, double latitude, double longitude, DateTime utcNow) =>
        new()
        {
            DisplayName = displayName,
            Latitude = latitude,
            Longitude = longitude,
            TemperatureC = TemperatureC,
            HumidityPercent = HumidityPercent,
            WeatherCode = WeatherCode,
            ObservedAt = ObservedAt,
            FetchedAtUtc = utcNow
        };
}

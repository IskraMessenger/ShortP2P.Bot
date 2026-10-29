namespace ShortP2P.Bot.Sample.Meteo.Weather;

/// <summary>Maps a Bright Sky condition and icon onto the WMO codes <c>MeteoTexts</c> already understands.</summary>
internal static class BrightSkyCodes
{
    public static int ToWeatherCode(string? condition, string? icon, double? cloudCover)
    {
        var phenomenon = Phenomenon(condition) ?? Phenomenon(icon);
        return phenomenon ?? FromCloudCover(cloudCover);
    }

    private static int? Phenomenon(string? value)
    {
        return Normalize(value) switch
        {
            "thunderstorm" => 95,
            "hail" => 96,
            "snow" => 71,
            "sleet" => 56,
            "rain" => 61,
            "fog" => 45,
            "clear-day" or "clear-night" => 0,
            "partly-cloudy-day" or "partly-cloudy-night" => 2,
            "cloudy" => 3,
            _ => null
        };
    }

    private static int FromCloudCover(double? cloudCover)
    {
        if (cloudCover is null)
            return 0;
        if (cloudCover >= 80)
            return 3;
        if (cloudCover >= 20)
            return 2;
        return 0;
    }

    private static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
}

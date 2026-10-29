namespace ShortP2P.Bot.Sample.Meteo.Weather;

/// <summary>
/// Selects the current-weather source. Open-Meteo looks up places itself.
/// Any other provider looks places up with OpenStreetMap Nominatim.
/// Bright Sky serves DWD stations, mostly in Germany.
/// </summary>
internal sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    public const string DefaultBrightSkyBaseUrl = "https://api.brightsky.dev/";

    public const string DefaultOpenStreetMapBaseUrl = "https://nominatim.openstreetmap.org/";

    /// <summary><c>OpenMeteo</c> or <c>BrightSky</c>. Empty means Open-Meteo.</summary>
    public string Provider { get; set; } = WeatherProviders.OpenMeteo;

    /// <summary>Absolute https base URL of a Bright Sky instance.</summary>
    public string BrightSkyBaseUrl { get; set; } = DefaultBrightSkyBaseUrl;

    /// <summary>Absolute https base URL of a Nominatim instance.</summary>
    public string OpenStreetMapBaseUrl { get; set; } = DefaultOpenStreetMapBaseUrl;

    public static Uri BrightSkyAddress(string? baseUrl)
    {
        var text = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBrightSkyBaseUrl : baseUrl.Trim();
        if (!text.EndsWith('/'))
            text += "/";

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Weather:BrightSkyBaseUrl must be an absolute https URL.");

        return uri;
    }

    public static Uri OpenStreetMapAddress(string? baseUrl)
    {
        var text = string.IsNullOrWhiteSpace(baseUrl) ? DefaultOpenStreetMapBaseUrl : baseUrl.Trim();
        if (!text.EndsWith('/'))
            text += "/";

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Weather:OpenStreetMapBaseUrl must be an absolute https URL.");

        return uri;
    }
}

internal enum WeatherProviderKind
{
    OpenMeteo,
    BrightSky
}

internal static class WeatherProviders
{
    public const string OpenMeteo = "OpenMeteo";
    public const string BrightSky = "BrightSky";

    public static WeatherProviderKind Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Trim().Equals(OpenMeteo, StringComparison.OrdinalIgnoreCase))
            return WeatherProviderKind.OpenMeteo;

        if (value.Trim().Equals(BrightSky, StringComparison.OrdinalIgnoreCase))
            return WeatherProviderKind.BrightSky;

        throw new InvalidOperationException(
            $"Weather:Provider '{value.Trim()}' is unknown. Use {OpenMeteo} or {BrightSky}.");
    }

    public static string Name(WeatherProviderKind kind) => kind switch
    {
        WeatherProviderKind.BrightSky => BrightSky,
        _ => OpenMeteo
    };
}

using System.Net;

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

    /// <summary>
    /// Optional absolute http/https proxy for weather API HttpClients.
    /// Empty, null, or omitted means direct (no proxy).
    /// </summary>
    public string? ProxyUrl { get; set; }

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

    /// <summary>
    /// Returns <c>null</c> when proxy is off; otherwise an absolute http or https URI.
    /// </summary>
    public static Uri? ProxyAddress(string? proxyUrl)
    {
        if (string.IsNullOrWhiteSpace(proxyUrl))
            return null;

        var text = proxyUrl.Trim();
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException(
                "Weather:ProxyUrl must be an absolute http or https URL, or empty to disable the proxy.");

        return uri;
    }

    /// <summary>
    /// Builds a weather HTTP handler. An empty <paramref name="proxyUrl"/> turns the proxy off.
    /// </summary>
    public static HttpClientHandler CreateHandler(string? proxyUrl)
    {
        var address = ProxyAddress(proxyUrl);
        if (address is null)
            return new HttpClientHandler { UseProxy = false };

        return new HttpClientHandler
        {
            Proxy = new WebProxy(address),
            UseProxy = true
        };
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

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal sealed class OpenMeteoClient
{
    private readonly HttpClient _http;

    public OpenMeteoClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<GeoHit?> SearchAsync(string name, string language, CancellationToken cancellationToken)
    {
        var url =
            "https://geocoding-api.open-meteo.com/v1/search?name="
            + Uri.EscapeDataString(name)
            + "&count=1&language="
            + GeocodingLanguage(language)
            + "&format=json";
        var response = await _http.GetFromJsonAsync<GeoResponse>(url, cancellationToken).ConfigureAwait(false);
        return First(response);
    }

    public async Task<GeoHit?> ReverseAsync(double latitude, double longitude, string language, CancellationToken cancellationToken)
    {
        var url =
            "https://geocoding-api.open-meteo.com/v1/reverse?latitude="
            + latitude.ToString(CultureInfo.InvariantCulture)
            + "&longitude="
            + longitude.ToString(CultureInfo.InvariantCulture)
            + "&language="
            + GeocodingLanguage(language)
            + "&format=json";
        var response = await _http.GetFromJsonAsync<GeoResponse>(url, cancellationToken).ConfigureAwait(false);
        return First(response);
    }

    public async Task<ForecastHit> CurrentAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var url =
            "https://api.open-meteo.com/v1/forecast?latitude="
            + latitude.ToString(CultureInfo.InvariantCulture)
            + "&longitude="
            + longitude.ToString(CultureInfo.InvariantCulture)
            + "&current=temperature_2m,relative_humidity_2m,weather_code&timezone=auto";
        var response = await _http.GetFromJsonAsync<ForecastResponse>(url, cancellationToken).ConfigureAwait(false);
        var current = response?.Current ?? throw new InvalidOperationException("Open-Meteo returned no current weather.");
        if (string.IsNullOrWhiteSpace(current.Time))
            throw new InvalidOperationException("Open-Meteo returned no observation time.");

        return new ForecastHit(
            current.Time.Replace('T', ' '),
            current.Temperature,
            (int)Math.Round(current.Humidity),
            current.WeatherCode);
    }

    private static GeoHit? First(GeoResponse? response)
    {
        var hit = response?.Results?.FirstOrDefault();
        if (hit is null || string.IsNullOrWhiteSpace(hit.Name))
            return null;

        var display = string.IsNullOrWhiteSpace(hit.Country) ? hit.Name : $"{hit.Name}, {hit.Country}";
        return new GeoHit(display, hit.Latitude, hit.Longitude);
    }

    private static string GeocodingLanguage(string language) => language == "cn" ? "zh" : language;

    private sealed class GeoResponse
    {
        public List<GeoResult>? Results { get; set; }
    }

    private sealed class GeoResult
    {
        public string? Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string? Country { get; set; }
    }

    private sealed class ForecastResponse
    {
        public ForecastCurrent? Current { get; set; }
    }

    private sealed class ForecastCurrent
    {
        public string? Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public double Humidity { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}

internal readonly record struct GeoHit(string DisplayName, double Latitude, double Longitude);

internal readonly record struct ForecastHit(string ObservedAt, double TemperatureC, int HumidityPercent, int WeatherCode);

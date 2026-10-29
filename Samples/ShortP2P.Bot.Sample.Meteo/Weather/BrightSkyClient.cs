using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal sealed class BrightSkyClient : ICurrentWeatherSource
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public BrightSkyClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<ForecastHit> CurrentAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var url = "current_weather?lat="
            + latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon="
            + longitude.ToString(CultureInfo.InvariantCulture);

        using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException(
                $"Bright Sky has no DWD station for {latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)}.");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Bright Sky returned {(int)response.StatusCode} for current weather.");

        var payload = await response.Content.ReadFromJsonAsync<CurrentResponse>(Json, cancellationToken).ConfigureAwait(false);
        var weather = payload?.Weather ?? throw new InvalidOperationException("Bright Sky returned no current weather.");
        if (weather.Temperature is null || weather.RelativeHumidity is null)
            throw new InvalidOperationException("Bright Sky returned no temperature or humidity.");

        var humidity = (int)Math.Round(weather.RelativeHumidity.Value, MidpointRounding.AwayFromZero);
        humidity = Math.Clamp(humidity, 0, 100);
        return new ForecastHit(
            FormatObservedAt(weather.Timestamp),
            weather.Temperature.Value,
            humidity,
            BrightSkyCodes.ToWeatherCode(weather.Condition, weather.Icon, weather.CloudCover));
    }

    private static string FormatObservedAt(string? timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return "";

        if (DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var instant))
            return instant.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        return timestamp.Trim().Replace('T', ' ');
    }

    private sealed class CurrentResponse
    {
        public CurrentWeather? Weather { get; set; }
    }

    private sealed class CurrentWeather
    {
        public string? Timestamp { get; set; }

        public double? Temperature { get; set; }

        [JsonPropertyName("relative_humidity")]
        public double? RelativeHumidity { get; set; }

        public string? Condition { get; set; }

        public string? Icon { get; set; }

        [JsonPropertyName("cloud_cover")]
        public double? CloudCover { get; set; }
    }
}

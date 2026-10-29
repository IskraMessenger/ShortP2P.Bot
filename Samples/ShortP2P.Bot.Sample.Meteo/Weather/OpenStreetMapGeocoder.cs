using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace ShortP2P.Bot.Sample.Meteo.Weather;

/// <summary>
/// Nominatim place search. The public instance allows at most one request per second,
/// so calls on this client wait for that gap.
/// </summary>
internal sealed class OpenStreetMapGeocoder : IPlaceGeocoder
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _nextUtc = DateTime.MinValue;

    public OpenStreetMapGeocoder(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<GeoHit?> SearchAsync(string name, string language, CancellationToken cancellationToken)
    {
        var url = "search?q="
            + Uri.EscapeDataString(name)
            + "&format=jsonv2&limit=1&addressdetails=1&accept-language="
            + AcceptLanguage(language);
        using var response = await SendAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenStreetMap search returned {(int)response.StatusCode}.");

        var places = await response.Content.ReadFromJsonAsync<List<NominatimPlace>>(Json, cancellationToken)
            .ConfigureAwait(false);
        return ToHit(places?.FirstOrDefault());
    }

    public async Task<GeoHit?> ReverseAsync(double latitude, double longitude, string language, CancellationToken cancellationToken)
    {
        var url = "reverse?lat="
            + latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon="
            + longitude.ToString(CultureInfo.InvariantCulture)
            + "&format=jsonv2&addressdetails=1&accept-language="
            + AcceptLanguage(language);
        using var response = await SendAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenStreetMap reverse geocoding returned {(int)response.StatusCode}.");

        var place = await response.Content.ReadFromJsonAsync<NominatimPlace>(Json, cancellationToken).ConfigureAwait(false);
        return ToHit(place);
    }

    private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var wait = _nextUtc - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);

            var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
            _nextUtc = DateTime.UtcNow.AddSeconds(1);
            return response;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static GeoHit? ToHit(NominatimPlace? place)
    {
        if (place is null || !string.IsNullOrWhiteSpace(place.Error))
            return null;
        if (!double.TryParse(place.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(place.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            return null;

        var display = Display(place);
        return string.IsNullOrWhiteSpace(display) ? null : new GeoHit(display, latitude, longitude);
    }

    private static string? Display(NominatimPlace place)
    {
        var locality = First(
            place.Address?.City,
            place.Address?.Town,
            place.Address?.Village,
            place.Address?.Municipality,
            place.Address?.Hamlet,
            place.Name);
        var country = place.Address?.Country?.Trim();
        if (!string.IsNullOrWhiteSpace(locality) && !string.IsNullOrWhiteSpace(country))
            return locality + ", " + country;
        if (!string.IsNullOrWhiteSpace(locality))
            return locality;
        return string.IsNullOrWhiteSpace(place.DisplayName) ? null : place.DisplayName.Trim();
    }

    private static string? First(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static string AcceptLanguage(string language) => language == "cn" ? "zh" : language;

    private sealed class NominatimPlace
    {
        public string? Lat { get; set; }

        public string? Lon { get; set; }

        public string? Name { get; set; }

        public string? DisplayName { get; set; }

        public string? Error { get; set; }

        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        public string? City { get; set; }

        public string? Town { get; set; }

        public string? Village { get; set; }

        public string? Municipality { get; set; }

        public string? Hamlet { get; set; }

        public string? Country { get; set; }
    }
}

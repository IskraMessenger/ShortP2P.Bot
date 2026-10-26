using System.Globalization;
using ShortP2P.Bot.Sample.Meteo.Storage;

namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal sealed class WeatherService
{
    private readonly OpenMeteoClient _meteo;
    private readonly SqliteWeatherCache _cache;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(OpenMeteoClient meteo, SqliteWeatherCache cache, ILogger<WeatherService> logger)
    {
        _meteo = meteo ?? throw new ArgumentNullException(nameof(meteo));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WeatherReport> GetAsync(string query, string language, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        if (GeoQuery.TryParseCoordinates(query, out var point))
            return await GetByCoordinatesAsync(point, language, utcNow, cancellationToken).ConfigureAwait(false);

        return await GetByPlaceAsync(query, language, utcNow, cancellationToken).ConfigureAwait(false);
    }

    private async Task<WeatherReport> GetByPlaceAsync(
        string query,
        string language,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var key = GeoQuery.NormalizePlaceKey(query);
        var cached = _cache.TryGetPlace(key, utcNow);
        if (cached is not null)
        {
            _logger.LogInformation("Weather cache hit for place {Place}", key);
            return cached;
        }

        var geo = await _meteo.SearchAsync(query.Trim(), language, cancellationToken).ConfigureAwait(false)
                  ?? throw new PlaceNotFoundException();
        var report = await FetchAsync(geo.DisplayName, geo.Latitude, geo.Longitude, utcNow, cancellationToken)
            .ConfigureAwait(false);
        _cache.SavePlace(key, report);
        _logger.LogInformation("Weather stored in cache for place {Place}", key);
        return report;
    }

    private async Task<WeatherReport> GetByCoordinatesAsync(
        GeoPoint point,
        string language,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var cached = _cache.TryGetCoordinates(point.Latitude, point.Longitude, utcNow);
        if (cached is not null)
        {
            _logger.LogInformation(
                "Weather cache hit within {RadiusKm} km of {Latitude}, {Longitude}",
                SqliteWeatherCache.RadiusKm,
                point.Latitude,
                point.Longitude);
            return cached;
        }

        GeoHit? geo = null;
        try
        {
            geo = await _meteo.ReverseAsync(point.Latitude, point.Longitude, language, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Reverse geocoding failed for {Latitude}, {Longitude}", point.Latitude, point.Longitude);
        }

        var display = geo?.DisplayName
                      ?? point.Latitude.ToString("0.####", CultureInfo.InvariantCulture)
                      + ", "
                      + point.Longitude.ToString("0.####", CultureInfo.InvariantCulture);
        var report = await FetchAsync(display, point.Latitude, point.Longitude, utcNow, cancellationToken)
            .ConfigureAwait(false);
        _cache.SaveCoordinates(report);
        _logger.LogInformation(
            "Weather stored in cache for coordinates {Latitude}, {Longitude}",
            point.Latitude,
            point.Longitude);
        return report;
    }

    private async Task<WeatherReport> FetchAsync(
        string displayName,
        double latitude,
        double longitude,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var forecast = await _meteo.CurrentAsync(latitude, longitude, cancellationToken).ConfigureAwait(false);
        return new WeatherReport
        {
            DisplayName = displayName,
            Latitude = latitude,
            Longitude = longitude,
            TemperatureC = forecast.TemperatureC,
            HumidityPercent = forecast.HumidityPercent,
            WeatherCode = forecast.WeatherCode,
            ObservedAt = forecast.ObservedAt,
            FetchedAtUtc = utcNow
        };
    }
}

internal sealed class PlaceNotFoundException : Exception;

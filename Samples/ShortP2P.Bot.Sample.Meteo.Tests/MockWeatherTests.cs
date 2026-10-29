using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShortP2P.Bot.Sample.Configuration;
using ShortP2P.Bot.Sample.Meteo.Commands;
using ShortP2P.Bot.Sample.Meteo.Storage;
using ShortP2P.Bot.Sample.Meteo.Weather;
using ShortP2P.Bot.Sample.Storage;

namespace ShortP2P.Bot.Sample.Meteo.Tests;

public sealed class MockWeatherTests
{
    [Fact]
    public void Place_UsesTheQueryAndFixedWeather()
    {
        var now = new DateTime(2026, 10, 29, 15, 0, 0, DateTimeKind.Utc);
        var report = MockWeather.ForPlace("  Берлин ", now);
        Assert.Equal("Берлин", report.DisplayName);
        Assert.Equal(MockWeather.TemperatureC, report.TemperatureC);
        Assert.Equal(MockWeather.HumidityPercent, report.HumidityPercent);
        Assert.Equal(MockWeather.WeatherCode, report.WeatherCode);
        Assert.Equal(MockWeather.ObservedAt, report.ObservedAt);

        var text = MeteoTexts.FormatReport(
            "ru",
            report.DisplayName,
            report.ObservedAt,
            report.TemperatureC,
            report.HumidityPercent,
            report.WeatherCode);
        Assert.Equal(
            "Берлин\nДата и время 2026-10-29 12:00\nТемпература: 8.2 °C\nВлажность: 70%\nУсловия: облачно",
            text);
    }

    [Fact]
    public void Coordinates_KeepTheParsedPoint()
    {
        var now = new DateTime(2026, 10, 29, 15, 0, 0, DateTimeKind.Utc);
        var report = MockWeather.ForCoordinates(new GeoPoint(52.52, 13.405), now);
        Assert.Equal("52.52, 13.405", report.DisplayName);
        Assert.Equal(52.52, report.Latitude);
        Assert.Equal(13.405, report.Longitude);
    }

    [Fact]
    public async Task Service_SkipsApisWhenBotIsMock()
    {
        var path = Path.Combine(Path.GetTempPath(), "meteo-mock-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var database = new SampleBotDatabase(Options.Create(new SampleBotOptions
            {
                Storage = new StorageOptions { ConnectionString = "Data Source=" + path },
                Bot = new BotOptions { IsMock = true }
            }));
            var service = new WeatherService(
                new BoomGeocoder(),
                new BoomWeather(),
                new SqliteWeatherCache(database),
                Options.Create(new WeatherOptions()),
                Options.Create(new SampleBotOptions { Bot = new BotOptions { IsMock = true } }),
                new SilentLogger());

            var report = await service.GetAsync("Berlin", "en", CancellationToken.None);
            Assert.Equal("Berlin", report.DisplayName);
            Assert.Equal(3, report.WeatherCode);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    private sealed class BoomGeocoder : IPlaceGeocoder
    {
        public Task<GeoHit?> SearchAsync(string name, string language, CancellationToken cancellationToken) =>
            Task.FromException<GeoHit?>(new InvalidOperationException("search"));

        public Task<GeoHit?> ReverseAsync(double latitude, double longitude, string language, CancellationToken cancellationToken) =>
            Task.FromException<GeoHit?>(new InvalidOperationException("reverse"));
    }

    private sealed class BoomWeather : ICurrentWeatherSource
    {
        public Task<ForecastHit> CurrentAsync(double latitude, double longitude, CancellationToken cancellationToken) =>
            Task.FromException<ForecastHit>(new InvalidOperationException("weather"));
    }

    private sealed class SilentLogger : ILogger<WeatherService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}

using Microsoft.Extensions.Options;
using ShortP2P.Bot.Sample.Configuration;
using ShortP2P.Bot.Sample.Meteo.Commands;
using ShortP2P.Bot.Sample.Meteo.Storage;
using ShortP2P.Bot.Sample.Meteo.Weather;
using ShortP2P.Bot.Sample.Storage;

namespace ShortP2P.Bot.Sample.Meteo.Tests;

public sealed class MeteoBehaviorTests
{
    [Fact]
    public void Parse_RecognizesCommands()
    {
        Assert.IsType<StartCommand>(CommandParser.Parse("/start"));
        Assert.IsType<StopCommand>(CommandParser.Parse("  /STOP  "));
        var lang = Assert.IsType<LangCommand>(CommandParser.Parse("/lang EN"));
        Assert.Equal("EN", lang.Code);
        var weather = Assert.IsType<WeatherCommand>(CommandParser.Parse("/weather  Moscow "));
        Assert.Equal("Moscow", weather.Query);
    }

    [Fact]
    public void Coordinates_ParsePairAndRejectPlaceName()
    {
        Assert.True(GeoQuery.TryParseCoordinates("55.75, 37.62", out var point));
        Assert.Equal(55.75, point.Latitude, 3);
        Assert.Equal(37.62, point.Longitude, 3);
        Assert.True(GeoQuery.TryParseCoordinates("55,75 37,62", out _));
        Assert.False(GeoQuery.TryParseCoordinates("Moscow", out _));
    }

    [Fact]
    public void Start_GreetsInRussianAndListsEveryLanguage()
    {
        var text = MeteoTexts.Start("ru");
        Assert.StartsWith("Привет, это тестовый бот для получения погоды.", text);
        Assert.Contains("/lang ru\n/lang en\n/lang es\n/lang de\n/lang fr\n/lang cn", text);
        Assert.Contains("/lang ru", MeteoTexts.Start("en"));
        Assert.Contains("/lang cn", MeteoTexts.Start("cn"));
    }

    [Fact]
    public void Report_UsesRussianLabels()
    {
        var text = MeteoTexts.FormatReport("ru", "Москва, Россия", "2026-10-26 14:00", 8.2, 70, 0);
        Assert.Equal(
            "Москва, Россия\nДата и время 2026-10-26 14:00\nТемпература: 8.2 °C\nВлажность: 70%\nУсловия: солнце",
            text);
    }

    [Fact]
    public void Cache_ReturnsPlaceAndNearbyCoordinatesWithinThirtyMinutes()
    {
        var path = Path.Combine(Path.GetTempPath(), "meteo-cache-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var database = new SampleBotDatabase(Options.Create(new SampleBotOptions
            {
                Storage = new StorageOptions { ConnectionString = "Data Source=" + path }
            }));
            var cache = new SqliteWeatherCache(database);
            var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            var report = new WeatherReport
            {
                DisplayName = "Moscow, Russia",
                Latitude = 55.7558,
                Longitude = 37.6173,
                TemperatureC = 8.2,
                HumidityPercent = 70,
                WeatherCode = 61,
                ObservedAt = "2026-10-26 15:00",
                FetchedAtUtc = now.AddMinutes(-10)
            };

            cache.SavePlace("moscow", report);
            cache.SaveCoordinates(report);

            var place = cache.TryGetPlace("moscow", now);
            Assert.NotNull(place);
            Assert.Equal(61, place.WeatherCode);

            var near = cache.TryGetCoordinates(55.7558 + 9.0 / 111.32, 37.6173, now);
            Assert.NotNull(near);

            var far = cache.TryGetCoordinates(55.7558 + 20.0 / 111.32, 37.6173, now);
            Assert.Null(far);

            Assert.Null(cache.TryGetPlace("moscow", now.AddMinutes(31)));
            Assert.Null(cache.TryGetCoordinates(55.7558, 37.6173, now.AddMinutes(31)));
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
}

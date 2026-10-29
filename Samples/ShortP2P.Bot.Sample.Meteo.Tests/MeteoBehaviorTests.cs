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
    public void Conditions_MapCloudAndSleet()
    {
        Assert.Equal("облачно", MeteoTexts.Condition("ru", 3));
        Assert.Equal("снег с дождём", MeteoTexts.Condition("ru", 66));
        Assert.Equal("cloudy", MeteoTexts.Condition("en", 2));
        Assert.Equal("snow and rain", MeteoTexts.Condition("en", 67));
        Assert.Equal("туман", MeteoTexts.Condition("ru", 45));
        Assert.Equal("гроза", MeteoTexts.Condition("ru", 95));
    }

    [Theory]
    [InlineData("ru", "Здравствуйте! Температура сегодня: 8,2 облачно. Влажность: 70. Всего доброго!")]
    [InlineData("en", "Hello! Temperature today: 8.2 cloudy. Humidity: 70. All the best!")]
    [InlineData("es", "¡Hola! Temperatura de hoy: 8,2 nublado. Humedad: 70. ¡Que le vaya bien!")]
    [InlineData("de", "Guten Tag! Temperatur heute: 8,2 bewölkt. Luftfeuchtigkeit: 70. Alles Gute!")]
    [InlineData("fr", "Bonjour ! Température aujourd'hui : 8,2 nuageux. Humidité : 70. Tous mes vœux !")]
    [InlineData("cn", "您好！今天的温度：8.2 多云。湿度：70。祝您一切顺利！")]
    public void VoiceScript_SpeaksWeatherInSelectedLanguage(string language, string expected)
    {
        Assert.Equal(expected, MeteoTexts.VoiceScript(language, 8.2, 70, 3));
    }

    [Theory]
    [InlineData("ru", 61, "дождь")]
    [InlineData("ru", 51, "дождь")]
    [InlineData("ru", 95, "дождь")]
    [InlineData("ru", 71, "снег")]
    [InlineData("ru", 75, "метель")]
    [InlineData("ru", 86, "метель")]
    [InlineData("ru", 66, "снег")]
    [InlineData("ru", 0, "ясно")]
    [InlineData("ru", 3, "облачно")]
    [InlineData("ru", 45, "облачно")]
    [InlineData("ru", 999, "ясно")]
    [InlineData("en", 75, "blizzard")]
    [InlineData("es", 61, "lluvia")]
    [InlineData("de", 71, "Schnee")]
    [InlineData("fr", 0, "dégagé")]
    [InlineData("cn", 3, "多云")]
    [InlineData("cn", 75, "暴风雪")]
    public void SpokenCondition_UsesNearestWeatherWord(string language, int weatherCode, string expected)
    {
        Assert.Equal(expected, MeteoTexts.SpokenCondition(language, weatherCode));
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

            cache.SavePlace("OpenMeteo", "moscow", report);
            cache.SaveCoordinates("OpenMeteo", report);

            var place = cache.TryGetPlace("OpenMeteo", "moscow", now);
            Assert.NotNull(place);
            Assert.Equal(61, place.WeatherCode);
            Assert.Null(cache.TryGetPlace("BrightSky", "moscow", now));

            var near = cache.TryGetCoordinates("OpenMeteo", 55.7558 + 9.0 / 111.32, 37.6173, now);
            Assert.NotNull(near);
            Assert.Null(cache.TryGetCoordinates("BrightSky", 55.7558, 37.6173, now));

            var far = cache.TryGetCoordinates("OpenMeteo", 55.7558 + 20.0 / 111.32, 37.6173, now);
            Assert.Null(far);

            Assert.Null(cache.TryGetPlace("OpenMeteo", "moscow", now.AddMinutes(31)));
            Assert.Null(cache.TryGetCoordinates("OpenMeteo", 55.7558, 37.6173, now.AddMinutes(31)));
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

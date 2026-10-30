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
    [InlineData("ru", "Москва", "Здравствуйте! Погода в Москва. Температура сегодня: восемь запятая два облачно. Влажность: семьдесят. Всего Вам доброго!")]
    [InlineData("en", "Moscow", "Hello! Weather in Moscow. Temperature today: eight point two cloudy. Humidity: seventy. All the best to you!")]
    [InlineData("es", "Moscú", "¡Hola! Tiempo en Moscú. Temperatura de hoy: ocho coma dos nublado. Humedad: setenta. ¡Que le vaya bien!")]
    [InlineData("de", "Moskau", "Guten Tag! Wetter in Moskau. Temperatur heute: acht Komma zwei bewölkt. Luftfeuchtigkeit: siebzig. Alles Gute für Sie!")]
    [InlineData("fr", "Moscou", "Bonjour ! Météo à Moscou. Température aujourd'hui : huit virgule deux nuageux. Humidité : soixante-dix. Je vous souhaite tout le meilleur !")]
    [InlineData("cn", "Moscow", "您好！Moscow的天气。今天的温度：八点二 多云。湿度：七十。祝您一切顺利！")]
    public void VoiceScript_SpeaksWeatherInSelectedLanguage(string language, string placeName, string expected)
    {
        var script = MeteoTexts.VoiceScript(language, placeName, 8.2, 70, 3);
        Assert.Equal(expected, script);
        Assert.StartsWith(MeteoTexts.VoiceGreeting(language), script);
        Assert.EndsWith(MeteoTexts.VoiceFarewell(language), script);
        if (language != "ru")
            Assert.DoesNotContain(script, letter => letter is >= '\u0400' and <= '\u04FF');
    }

    [Theory]
    [InlineData("en", 21, "twenty one")]
    [InlineData("en", 100, "one hundred")]
    [InlineData("ru", 21, "двадцать один")]
    [InlineData("ru", 100, "сто")]
    [InlineData("es", 16, "dieciséis")]
    [InlineData("es", 22, "veintidós")]
    [InlineData("es", 100, "cien")]
    [InlineData("de", 21, "einundzwanzig")]
    [InlineData("de", 100, "hundert")]
    [InlineData("fr", 21, "vingt et un")]
    [InlineData("fr", 71, "soixante et onze")]
    [InlineData("fr", 80, "quatre-vingts")]
    [InlineData("fr", 100, "cent")]
    [InlineData("cn", 10, "十")]
    [InlineData("cn", 70, "七十")]
    [InlineData("cn", 110, "一百一十")]
    public void SpokenNumber_UsesTheSelectedLanguage(string language, int value, string expected)
    {
        Assert.Equal(expected, SpokenNumber.Cardinal(language, value));
    }

    [Fact]
    public void Pronounceable_KeepsOneLanguageForTheInstalledVoice()
    {
        var russian = MeteoTexts.VoiceScript("ru", "Москва", 8.2, 70, 3);
        var spokenRussian = MeteoTexts.Pronounceable("ru", russian);
        Assert.Equal(
            "Zdravstvuyte! Pogoda v Moskva. Temperatura segodnya: vosem zapyataya dva oblachno. Vlazhnost: semdesyat. Vsego Vam dobrogo!",
            spokenRussian);
        Assert.DoesNotContain(spokenRussian, letter => letter is >= '\u0400' and <= '\u04FF');

        var chinese = MeteoTexts.VoiceScript("cn", "Moscow", 8.2, 70, 3);
        var spokenChinese = MeteoTexts.Pronounceable("cn", chinese);
        Assert.Equal(
            "nin hao!Moscow de tian qi. jin tian de wen du: ba dian er duo yun. shi du: qi shi. zhu nin yi qie shun li!",
            spokenChinese);
        Assert.DoesNotContain(spokenChinese, letter => letter is >= '\u4E00' and <= '\u9FFF');

        var english = MeteoTexts.VoiceScript("en", "Moscow", -3.5, 0, 0);
        Assert.Equal(
            "Hello! Weather in Moscow. Temperature today: minus three point five clear. Humidity: zero. All the best to you!",
            english);
        Assert.Equal(english, MeteoTexts.Pronounceable("en", english));
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

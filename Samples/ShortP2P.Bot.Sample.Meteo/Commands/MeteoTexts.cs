using System.Globalization;
using System.Text;

namespace ShortP2P.Bot.Sample.Meteo.Commands;

internal static class MeteoTexts
{
    public const string DefaultLanguage = "ru";

    public static readonly string[] Languages = ["ru", "en", "es", "de", "fr", "cn"];

    public static string LangList { get; } = string.Join('\n', Languages.Select(code => "/lang " + code));

    public static bool TryNormalizeLanguage(string? code, out string language)
    {
        language = (code ?? "").Trim().ToLowerInvariant();
        if (Languages.Contains(language))
            return true;

        language = "";
        return false;
    }

    public static string Start(string language) =>
        Greeting(language) + "\n" + LangList;

    public static string NeedStart(string language) => language switch
    {
        "en" => "Send /start to begin.",
        "es" => "Envía /start para empezar.",
        "de" => "Sende /start, um zu beginnen.",
        "fr" => "Envoyez /start pour commencer.",
        "cn" => "发送 /start 开始。",
        _ => "Отправь /start, чтобы начать."
    };

    public static string Stopped(string language) => language switch
    {
        "en" => "The bot is stopped. Send /start to begin again.",
        "es" => "El bot está detenido. Envía /start para empezar de nuevo.",
        "de" => "Der Bot ist gestoppt. Sende /start, um neu zu beginnen.",
        "fr" => "Le bot est arrêté. Envoyez /start pour recommencer.",
        "cn" => "机器人已停止。发送 /start 重新开始。",
        _ => "Бот остановлен. Отправь /start, чтобы начать снова."
    };

    public static string LanguageChanged(string language) => language switch
    {
        "en" => "Language switched to English.",
        "es" => "Idioma cambiado a español.",
        "de" => "Sprache auf Deutsch umgestellt.",
        "fr" => "Langue changée en français.",
        "cn" => "语言已切换为中文。",
        _ => "Язык переключён на русский."
    };

    public static string UnknownLanguage(string language) =>
        (language switch
        {
            "en" => "Unknown language. Enter a command:",
            "es" => "Idioma desconocido. Introduce el comando:",
            "de" => "Unbekannte Sprache. Gib den Befehl ein:",
            "fr" => "Langue inconnue. Saisissez la commande :",
            "cn" => "未知语言。请输入命令：",
            _ => "Неизвестный язык. Вбей команду:"
        }) + "\n" + LangList;

    public static string WeatherUsage(string language) => language switch
    {
        "en" => "Enter a place or coordinates: /weather Moscow or /weather 55.75, 37.62",
        "es" => "Indica un lugar o coordenadas: /weather Madrid o /weather 40.42, -3.70",
        "de" => "Gib einen Ort oder Koordinaten ein: /weather Berlin oder /weather 52.52, 13.40",
        "fr" => "Indiquez un lieu ou des coordonnées : /weather Paris ou /weather 48.86, 2.35",
        "cn" => "请输入地点或坐标：/weather 北京 或 /weather 39.90, 116.40",
        _ => "Укажи населённый пункт или координаты: /weather Москва или /weather 55.75, 37.62"
    };

    public static string PlaceNotFound(string language) => language switch
    {
        "en" => "Place not found.",
        "es" => "Lugar no encontrado.",
        "de" => "Ort nicht gefunden.",
        "fr" => "Lieu introuvable.",
        "cn" => "未找到该地点。",
        _ => "Населённый пункт не найден."
    };

    public static string WeatherUnavailable(string language) => language switch
    {
        "en" => "Weather is temporarily unavailable.",
        "es" => "El tiempo no está disponible temporalmente.",
        "de" => "Das Wetter ist vorübergehend nicht verfügbar.",
        "fr" => "La météo est temporairement indisponible.",
        "cn" => "天气暂时无法获取。",
        _ => "Погода временно недоступна."
    };

    public static string InvalidCoordinates(string language) => language switch
    {
        "en" => "Coordinates are invalid. Use latitude, longitude.",
        "es" => "Coordenadas no válidas. Usa latitud, longitud.",
        "de" => "Koordinaten sind ungültig. Verwende Breitengrad, Längengrad.",
        "fr" => "Coordonnées invalides. Utilisez latitude, longitude.",
        "cn" => "坐标无效。请使用纬度, 经度。",
        _ => "Координаты неверные. Нужны широта, долгота."
    };

    public static string Help(string language) => language switch
    {
        "en" => "Commands: /start, /stop, /lang, /weather <place or coordinates>",
        "es" => "Comandos: /start, /stop, /lang, /weather <lugar o coordenadas>",
        "de" => "Befehle: /start, /stop, /lang, /weather <Ort oder Koordinaten>",
        "fr" => "Commandes : /start, /stop, /lang, /weather <lieu ou coordonnées>",
        "cn" => "命令：/start、/stop、/lang、/weather <地点或坐标>",
        _ => "Команды: /start, /stop, /lang, /weather <населённый пункт или координаты>"
    };

    public static string FormatReport(
        string language,
        string place,
        string observedAt,
        double temperatureC,
        int humidityPercent,
        int weatherCode)
    {
        var (dateLabel, tempLabel, humidityLabel, conditionsLabel) = Labels(language);
        var temperature = temperatureC.ToString("0.0", CultureInfo.InvariantCulture);
        return string.Join('\n',
            place,
            $"{dateLabel} {observedAt}",
            $"{tempLabel}: {temperature} °C",
            $"{humidityLabel}: {humidityPercent}%",
            $"{conditionsLabel}: {Condition(language, weatherCode)}");
    }

    public static string Condition(string language, int weatherCode)
    {
        var key = ConditionKey(weatherCode);
        return (language, key) switch
        {
            ("en", "sun") => "sun",
            ("en", "dry") => "dry",
            ("en", "rain") => "rain",
            ("en", "drizzle") => "drizzle",
            ("en", "snow") => "snow",
            ("en", "fog") => "fog",
            ("en", "thunderstorm") => "thunderstorm",
            ("en", "cloudy") => "cloudy",
            ("en", "sleet") => "snow and rain",
            ("es", "sun") => "sol",
            ("es", "dry") => "seco",
            ("es", "rain") => "lluvia",
            ("es", "drizzle") => "llovizna",
            ("es", "snow") => "nieve",
            ("es", "fog") => "niebla",
            ("es", "thunderstorm") => "tormenta",
            ("es", "cloudy") => "nublado",
            ("es", "sleet") => "nieve y lluvia",
            ("de", "sun") => "Sonne",
            ("de", "dry") => "trocken",
            ("de", "rain") => "Regen",
            ("de", "drizzle") => "Nieselregen",
            ("de", "snow") => "Schnee",
            ("de", "fog") => "Nebel",
            ("de", "thunderstorm") => "Gewitter",
            ("de", "cloudy") => "bewölkt",
            ("de", "sleet") => "Schnee und Regen",
            ("fr", "sun") => "soleil",
            ("fr", "dry") => "sec",
            ("fr", "rain") => "pluie",
            ("fr", "drizzle") => "bruine",
            ("fr", "snow") => "neige",
            ("fr", "fog") => "brouillard",
            ("fr", "thunderstorm") => "orage",
            ("fr", "cloudy") => "nuageux",
            ("fr", "sleet") => "neige et pluie",
            ("cn", "sun") => "晴",
            ("cn", "dry") => "干燥",
            ("cn", "rain") => "雨",
            ("cn", "drizzle") => "毛毛雨",
            ("cn", "snow") => "雪",
            ("cn", "fog") => "雾",
            ("cn", "thunderstorm") => "雷暴",
            ("cn", "cloudy") => "多云",
            ("cn", "sleet") => "雨夹雪",
            (_, "sun") => "солнце",
            (_, "dry") => "сухо",
            (_, "rain") => "дождь",
            (_, "drizzle") => "морось",
            (_, "snow") => "снег",
            (_, "fog") => "туман",
            (_, "thunderstorm") => "гроза",
            (_, "cloudy") => "облачно",
            (_, "sleet") => "снег с дождём",
            _ => "сухо"
        };
    }

    public static string ConditionKey(int weatherCode) => weatherCode switch
    {
        0 or 1 => "sun",
        2 or 3 => "cloudy",
        45 or 48 => "fog",
        51 or 53 or 55 => "drizzle",
        56 or 57 or 66 or 67 => "sleet",
        61 or 63 or 65 or 80 or 81 or 82 => "rain",
        71 or 73 or 75 or 77 or 85 or 86 => "snow",
        95 or 96 or 99 => "thunderstorm",
        _ => "dry"
    };

    public static string VoiceGreeting(string language) => language switch
    {
        "en" => "Hello!",
        "es" => "¡Hola!",
        "de" => "Guten Tag!",
        "fr" => "Bonjour !",
        "cn" => "您好！",
        _ => "Здравствуйте!"
    };

    public static string VoiceFarewell(string language) => language switch
    {
        "en" => "All the best to you!",
        "es" => "¡Que le vaya bien!",
        "de" => "Alles Gute für Sie!",
        "fr" => "Je vous souhaite tout le meilleur !",
        "cn" => "祝您一切顺利！",
        _ => "Всего Вам доброго!"
    };

    /// <summary>
    /// One of five spoken conditions, in the selected language.
    /// Other forecast keys use the nearest of these: drizzle and thunderstorm follow rain,
    /// fog follows cloudy, sleet follows snow, dry follows clear, heavy snow is a blizzard.
    /// </summary>
    public static string SpokenCondition(string language, int weatherCode)
    {
        var spoken = SpokenKey(weatherCode);
        return (language, spoken) switch
        {
            ("en", "rain") => "rain",
            ("en", "snow") => "snow",
            ("en", "blizzard") => "blizzard",
            ("en", "clear") => "clear",
            ("en", "cloudy") => "cloudy",
            ("es", "rain") => "lluvia",
            ("es", "snow") => "nieve",
            ("es", "blizzard") => "ventisca",
            ("es", "clear") => "despejado",
            ("es", "cloudy") => "nublado",
            ("de", "rain") => "Regen",
            ("de", "snow") => "Schnee",
            ("de", "blizzard") => "Schneesturm",
            ("de", "clear") => "klar",
            ("de", "cloudy") => "bewölkt",
            ("fr", "rain") => "pluie",
            ("fr", "snow") => "neige",
            ("fr", "blizzard") => "blizzard",
            ("fr", "clear") => "dégagé",
            ("fr", "cloudy") => "nuageux",
            ("cn", "rain") => "雨",
            ("cn", "snow") => "雪",
            ("cn", "blizzard") => "暴风雪",
            ("cn", "clear") => "晴",
            ("cn", "cloudy") => "多云",
            (_, "rain") => "дождь",
            (_, "snow") => "снег",
            (_, "blizzard") => "метель",
            (_, "clear") => "ясно",
            (_, "cloudy") => "облачно",
            _ => "ясно"
        };
    }

    /// <summary>
    /// Greeting, place, then today's temperature, condition and humidity, then farewell.
    /// Every word, including the numbers, is in <paramref name="language"/>.
    /// <paramref name="placeName"/> is the display name already resolved for that language.
    /// </summary>
    public static string VoiceScript(
        string language,
        string placeName,
        double temperatureC,
        int humidityPercent,
        int weatherCode)
    {
        var temperature = SpokenNumber.Temperature(language, temperatureC);
        var humidity = SpokenNumber.Cardinal(language, humidityPercent);
        var condition = SpokenCondition(language, weatherCode);
        var greeting = VoiceGreeting(language);
        var farewell = VoiceFarewell(language);
        var place = placeName.Trim();
        return language switch
        {
            "en" => $"{greeting} Weather in {place}. Temperature today: {temperature} {condition}. Humidity: {humidity}. {farewell}",
            "es" => $"{greeting} Tiempo en {place}. Temperatura de hoy: {temperature} {condition}. Humedad: {humidity}. {farewell}",
            "de" => $"{greeting} Wetter in {place}. Temperatur heute: {temperature} {condition}. Luftfeuchtigkeit: {humidity}. {farewell}",
            "fr" => $"{greeting} Météo à {place}. Température aujourd'hui : {temperature} {condition}. Humidité : {humidity}. {farewell}",
            "cn" => $"{greeting}{place}的天气。今天的温度：{temperature} {condition}。湿度：{humidity}。{farewell}",
            _ => $"{greeting} Погода в {place}. Температура сегодня: {temperature} {condition}. Влажность: {humidity}. {farewell}"
        };
    }

    /// <summary>
    /// Same sentence as <paramref name="script"/>, rewritten so one voice can say all of it.
    /// Cyrillic and Chinese characters are dropped by an English voice, so those scripts are
    /// spoken from a phonetic spelling. Latin-script languages stay as written.
    /// </summary>
    public static string Pronounceable(string language, string script) => language switch
    {
        "ru" => TransliterateRussian(script),
        "cn" => ChinesePinyin(script),
        _ => script
    };

    /// <summary>Heavy snow is spoken as a blizzard. Every other code follows <see cref="ConditionKey"/>.</summary>
    public static string SpokenKey(int weatherCode)
    {
        if (weatherCode is 75 or 86)
            return "blizzard";

        return ConditionKey(weatherCode) switch
        {
            "sun" or "dry" => "clear",
            "cloudy" or "fog" => "cloudy",
            "rain" or "drizzle" or "thunderstorm" => "rain",
            "snow" or "sleet" => "snow",
            _ => "clear"
        };
    }

    private static string TransliterateRussian(string text)
    {
        var builder = new StringBuilder(text.Length * 2);
        foreach (var letter in text)
        {
            if (!RussianLetters.TryGetValue(char.ToLowerInvariant(letter), out var latin))
            {
                builder.Append(letter);
                continue;
            }

            if (latin.Length == 0)
                continue;

            if (char.IsUpper(letter))
                latin = char.ToUpperInvariant(latin[0]) + latin[1..];
            builder.Append(latin);
        }

        return builder.ToString();
    }

    private static readonly Dictionary<char, string> RussianLetters = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "yo",
        ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
        ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "shch",
        ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
    };

    private static string ChinesePinyin(string text)
    {
        var builder = new StringBuilder(text.Length * 2);
        foreach (var letter in text)
        {
            if (ChineseSyllables.TryGetValue(letter, out var syllable))
            {
                if (builder.Length > 0 && builder[^1] != ' ')
                    builder.Append(' ');
                builder.Append(syllable);
                continue;
            }

            builder.Append(letter switch
            {
                '！' => '!',
                '。' => '.',
                '：' => ':',
                '，' or '、' => ',',
                _ => letter
            });
        }

        return string.Join(' ', builder.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static readonly Dictionary<char, string> ChineseSyllables = new()
    {
        ['您'] = "nin", ['好'] = "hao", ['今'] = "jin", ['天'] = "tian", ['的'] = "de",
        ['气'] = "qi", ['温'] = "wen", ['度'] = "du", ['湿'] = "shi", ['雨'] = "yu", ['雪'] = "xue",
        ['暴'] = "bao", ['风'] = "feng", ['晴'] = "qing", ['多'] = "duo", ['云'] = "yun",
        ['北'] = "bei", ['京'] = "jing",
        ['祝'] = "zhu", ['一'] = "yi", ['切'] = "qie", ['顺'] = "shun", ['利'] = "li",
        ['零'] = "ling", ['二'] = "er", ['三'] = "san", ['四'] = "si", ['五'] = "wu",
        ['六'] = "liu", ['七'] = "qi", ['八'] = "ba", ['九'] = "jiu", ['十'] = "shi",
        ['百'] = "bai", ['点'] = "dian", ['负'] = "fu"
    };

    private static string Greeting(string language) => language switch
    {
        "en" => "Hello, this is a test bot for getting the weather. To choose a language, enter a command:",
        "es" => "Hola, este es un bot de prueba para obtener el tiempo. Para elegir el idioma, introduce el comando:",
        "de" => "Hallo, das ist ein Testbot zum Abrufen des Wetters. Zum Auswählen der Sprache gib den Befehl ein:",
        "fr" => "Bonjour, ceci est un bot de test pour obtenir la météo. Pour choisir la langue, saisissez la commande :",
        "cn" => "你好，这是一个用于查询天气的测试机器人。要选择语言，请输入命令：",
        _ => "Привет, это тестовый бот для получения погоды. Для выбора языка вбей команду:"
    };

    private static (string Date, string Temperature, string Humidity, string Conditions) Labels(string language) =>
        language switch
        {
            "en" => ("Date and time", "Temperature", "Humidity", "Conditions"),
            "es" => ("Fecha y hora", "Temperatura", "Humedad", "Condiciones"),
            "de" => ("Datum und Uhrzeit", "Temperatur", "Luftfeuchtigkeit", "Bedingungen"),
            "fr" => ("Date et heure", "Température", "Humidité", "Conditions"),
            "cn" => ("日期和时间", "温度", "湿度", "天气"),
            _ => ("Дата и время", "Температура", "Влажность", "Условия")
        };
}

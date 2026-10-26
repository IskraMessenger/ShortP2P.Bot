using System.Globalization;

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
            ("es", "sun") => "sol",
            ("es", "dry") => "seco",
            ("es", "rain") => "lluvia",
            ("es", "drizzle") => "llovizna",
            ("es", "snow") => "nieve",
            ("es", "fog") => "niebla",
            ("es", "thunderstorm") => "tormenta",
            ("de", "sun") => "Sonne",
            ("de", "dry") => "trocken",
            ("de", "rain") => "Regen",
            ("de", "drizzle") => "Nieselregen",
            ("de", "snow") => "Schnee",
            ("de", "fog") => "Nebel",
            ("de", "thunderstorm") => "Gewitter",
            ("fr", "sun") => "soleil",
            ("fr", "dry") => "sec",
            ("fr", "rain") => "pluie",
            ("fr", "drizzle") => "bruine",
            ("fr", "snow") => "neige",
            ("fr", "fog") => "brouillard",
            ("fr", "thunderstorm") => "orage",
            ("cn", "sun") => "晴",
            ("cn", "dry") => "干燥",
            ("cn", "rain") => "雨",
            ("cn", "drizzle") => "毛毛雨",
            ("cn", "snow") => "雪",
            ("cn", "fog") => "雾",
            ("cn", "thunderstorm") => "雷暴",
            (_, "sun") => "солнце",
            (_, "dry") => "сухо",
            (_, "rain") => "дождь",
            (_, "drizzle") => "морось",
            (_, "snow") => "снег",
            (_, "fog") => "туман",
            (_, "thunderstorm") => "гроза",
            _ => "сухо"
        };
    }

    public static string ConditionKey(int weatherCode) => weatherCode switch
    {
        0 or 1 => "sun",
        2 or 3 => "dry",
        45 or 48 => "fog",
        51 or 53 or 55 or 56 or 57 => "drizzle",
        61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "rain",
        71 or 73 or 75 or 77 or 85 or 86 => "snow",
        95 or 96 or 99 => "thunderstorm",
        _ => "dry"
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

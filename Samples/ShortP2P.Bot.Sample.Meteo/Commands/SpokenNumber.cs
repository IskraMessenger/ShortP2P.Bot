using System.Globalization;

namespace ShortP2P.Bot.Sample.Meteo.Commands;

/// <summary>Speaks a weather number as words so the sentence stays in the selected language.</summary>
internal static class SpokenNumber
{
    public static string Temperature(string language, double celsius)
    {
        var text = celsius.ToString("0.0", CultureInfo.InvariantCulture);
        var negative = text[0] == '-';
        if (negative)
            text = text[1..];

        var dot = text.IndexOf('.');
        var whole = int.Parse(text[..dot], CultureInfo.InvariantCulture);
        var tenth = int.Parse(text[(dot + 1)..], CultureInfo.InvariantCulture);
        if (language == "cn")
        {
            var body = Cardinal(language, whole) + "点" + Digit(language, tenth);
            return negative ? "负" + body : body;
        }

        var spoken = Cardinal(language, whole) + DecimalSeparator(language) + Digit(language, tenth);
        return negative ? Minus(language) + " " + spoken : spoken;
    }

    public static string Cardinal(string language, int value)
    {
        if (value < 0)
            return Minus(language) + " " + Cardinal(language, -value);

        return language switch
        {
            "en" => English(value),
            "es" => Spanish(value),
            "de" => German(value),
            "fr" => French(value),
            "cn" => Chinese(value),
            _ => Russian(value)
        };
    }

    private static string Digit(string language, int tenth) =>
        language == "cn" ? Chinese(tenth) : Cardinal(language, tenth);

    private static string Minus(string language) => language switch
    {
        "en" => "minus",
        "es" => "menos",
        "de" => "minus",
        "fr" => "moins",
        "cn" => "负",
        _ => "минус"
    };

    private static string DecimalSeparator(string language) => language switch
    {
        "en" => " point ",
        "es" => " coma ",
        "de" => " Komma ",
        "fr" => " virgule ",
        _ => " запятая "
    };

    private static string English(int value)
    {
        string[] below20 =
        [
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
            "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
        ];
        string[] tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];
        if (value < 20)
            return below20[value];
        if (value < 100)
            return Join(tens[value / 10], value % 10 == 0 ? null : below20[value % 10]);
        if (value < 1000)
        {
            var head = below20[value / 100] + " hundred";
            return value % 100 == 0 ? head : head + " " + English(value % 100);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Russian(int value)
    {
        string[] below20 =
        [
            "ноль", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять",
            "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
            "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
        ];
        string[] tens = ["", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто"];
        string[] hundreds = ["", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"];
        if (value < 20)
            return below20[value];
        if (value < 100)
            return Join(tens[value / 10], value % 10 == 0 ? null : below20[value % 10]);
        if (value < 1000)
            return Join(hundreds[value / 100], value % 100 == 0 ? null : Russian(value % 100));

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Spanish(int value)
    {
        string[] below16 =
        [
            "cero", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
            "diez", "once", "doce", "trece", "catorce", "quince"
        ];
        if (value < 16)
            return below16[value];
        if (value < 20)
            return value switch { 16 => "dieciséis", 17 => "diecisiete", 18 => "dieciocho", _ => "diecinueve" };
        if (value < 30)
        {
            string[] twenties =
            [
                "veinte", "veintiuno", "veintidós", "veintitrés", "veinticuatro",
                "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve"
            ];
            return twenties[value - 20];
        }

        string[] tens = ["", "", "", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"];
        if (value < 100)
        {
            var unit = value % 10;
            var ten = tens[value / 10];
            return unit == 0 ? ten : ten + " y " + (unit == 1 ? "uno" : below16[unit]);
        }

        if (value == 100)
            return "cien";
        if (value < 1000)
        {
            string[] hundreds =
            [
                "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
                "seiscientos", "setecientos", "ochocientos", "novecientos"
            ];
            var rest = value % 100;
            return hundreds[value / 100] + (rest == 0 ? "" : " " + Spanish(rest));
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string German(int value)
    {
        string[] below20 =
        [
            "null", "eins", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht", "neun",
            "zehn", "elf", "zwölf", "dreizehn", "vierzehn", "fünfzehn", "sechzehn", "siebzehn", "achtzehn", "neunzehn"
        ];
        string[] tens = ["", "", "zwanzig", "dreißig", "vierzig", "fünfzig", "sechzig", "siebzig", "achtzig", "neunzig"];
        if (value < 20)
            return below20[value];
        if (value < 100)
        {
            var unit = value % 10;
            var ten = tens[value / 10];
            if (unit == 0)
                return ten;
            return (unit == 1 ? "ein" : below20[unit]) + "und" + ten;
        }

        if (value < 1000)
        {
            var head = value / 100 == 1 ? "hundert" : below20[value / 100] + "hundert";
            return value % 100 == 0 ? head : head + German(value % 100);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string French(int value)
    {
        string[] below20 =
        [
            "zéro", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf",
            "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf"
        ];
        if (value < 20)
            return below20[value];
        if (value < 70)
        {
            string[] tens = ["", "", "vingt", "trente", "quarante", "cinquante", "soixante"];
            return FrenchTen(tens[value / 10], value % 10, below20, etForOne: true);
        }

        if (value < 80)
        {
            var rest = value - 60;
            return rest == 11 ? "soixante et onze" : "soixante-" + below20[rest];
        }

        if (value < 100)
        {
            var rest = value - 80;
            return rest == 0 ? "quatre-vingts" : "quatre-vingt-" + below20[rest];
        }

        if (value == 100)
            return "cent";
        if (value < 200)
            return "cent " + French(value - 100);
        if (value < 1000)
        {
            var hundreds = value / 100;
            var rest = value % 100;
            var head = below20[hundreds] + (rest == 0 ? " cents" : " cent");
            return rest == 0 ? head : head + " " + French(rest);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FrenchTen(string ten, int unit, string[] below20, bool etForOne)
    {
        if (unit == 0)
            return ten;
        if (unit == 1 && etForOne)
            return ten + " et un";
        return ten + "-" + below20[unit];
    }

    private static string Chinese(int value)
    {
        string[] digit = ["零", "一", "二", "三", "四", "五", "六", "七", "八", "九"];
        if (value < 10)
            return digit[value];
        if (value < 20)
            return value == 10 ? "十" : "十" + digit[value - 10];
        if (value < 100)
        {
            var unit = value % 10;
            return digit[value / 10] + "十" + (unit == 0 ? "" : digit[unit]);
        }

        if (value < 1000)
        {
            var head = digit[value / 100] + "百";
            var rest = value % 100;
            if (rest == 0)
                return head;
            if (rest < 10)
                return head + "零" + digit[rest];
            if (rest < 20)
                return head + "一" + Chinese(rest);
            return head + Chinese(rest);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(string left, string? right) =>
        string.IsNullOrEmpty(right) ? left : left + " " + right;
}

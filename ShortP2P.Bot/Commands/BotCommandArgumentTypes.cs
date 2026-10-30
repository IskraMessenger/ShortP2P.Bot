using System.Globalization;

namespace ShortP2P.Bot.Commands;

internal static class BotCommandArgumentTypes
{
    public const string Dynamic = "dynamic";
    public const string String = "string";
    public const string Number = "number";
    public const string DateTime = "datetime";

    public static bool TryCanonical(string? type, out string canonical)
    {
        canonical = "";
        if (string.IsNullOrWhiteSpace(type))
            return false;

        var trimmed = type.Trim();
        if (trimmed.Equals(Dynamic, StringComparison.OrdinalIgnoreCase))
            canonical = Dynamic;
        else if (trimmed.Equals(String, StringComparison.OrdinalIgnoreCase))
            canonical = String;
        else if (trimmed.Equals(Number, StringComparison.OrdinalIgnoreCase))
            canonical = Number;
        else if (trimmed.Equals(DateTime, StringComparison.OrdinalIgnoreCase))
            canonical = DateTime;
        else
            return false;

        return true;
    }

    public static bool ValueMatches(string canonicalType, string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return canonicalType switch
        {
            Dynamic => true,
            String => true,
            Number => IsNumber(value),
            DateTime => IsDateTime(value),
            _ => false
        };
    }

    private static bool IsNumber(string value)
    {
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        return decimal.TryParse(value, styles, CultureInfo.InvariantCulture, out _)
            || decimal.TryParse(value, styles, CultureInfo.CurrentCulture, out _);
    }

    private static readonly string[] DateTimeFormats =
    {
        "yyyy-MM-dd",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "dd.MM.yyyy",
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy HH:mm:ss",
        "dd/MM/yyyy",
        "dd/MM/yyyy HH:mm",
        "d MMMM yyyy"
    };

    private static bool IsDateTime(string value)
    {
        const DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;
        if (System.DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, styles, out _))
            return true;
        if (System.DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out _))
            return true;
        if (System.DateTime.TryParse(value, CultureInfo.CurrentCulture, styles, out _))
            return true;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, styles, out _)
            || DateTimeOffset.TryParse(value, CultureInfo.CurrentCulture, styles, out _);
    }
}

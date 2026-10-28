using System.Globalization;
using System.Text.RegularExpressions;

namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal readonly record struct GeoPoint(double Latitude, double Longitude);

internal static partial class GeoQuery
{
    [GeneratedRegex(@"^\s*([+-]?\d+(?:\.\d+)?)\s*[,;]\s*([+-]?\d+(?:\.\d+)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex CommaPair();

    [GeneratedRegex(@"^\s*([+-]?\d+(?:\.\d+)?)\s+([+-]?\d+(?:\.\d+)?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex SpacePair();

    public static string NormalizePlaceKey(string name)
    {
        var parts = name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts).ToLowerInvariant();
    }

    public static bool TryParseCoordinates(string text, out GeoPoint point)
    {
        point = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var match = CommaPair().Match(text);
        if (!match.Success)
            match = SpacePair().Match(text);
        if (!match.Success && TryCommaDecimals(text, out point))
            return true;
        if (!match.Success)
            return false;

        if (!double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            return false;

        if (!IsValid(lat, lon))
            return false;

        point = new GeoPoint(lat, lon);
        return true;
    }

    public static double Kilometers(GeoPoint left, GeoPoint right)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(right.Latitude - left.Latitude);
        var dLon = ToRadians(right.Longitude - left.Longitude);
        var lat1 = ToRadians(left.Latitude);
        var lat2 = ToRadians(right.Latitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static bool TryCommaDecimals(string text, out GeoPoint point)
    {
        point = default;
        var parts = text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        var latText = parts[0].Trim().TrimEnd(',').Replace(',', '.');
        var lonText = parts[1].Trim().TrimEnd(',').Replace(',', '.');
        if (!double.TryParse(latText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
            || !double.TryParse(lonText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon)
            || !IsValid(lat, lon))
            return false;

        point = new GeoPoint(lat, lon);
        return true;
    }

    private static bool IsValid(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}

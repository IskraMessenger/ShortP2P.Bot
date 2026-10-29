namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>Writes generated weather PNG and m4a files under a local <c>saved</c> folder for inspection.</summary>
internal static class WeatherMediaArchive
{
    public const string FolderName = "saved";

    public static string RootDirectory =>
        Path.Combine(AppContext.BaseDirectory, FolderName);

    public static IReadOnlyList<string> Save(
        string conditionKey,
        string language,
        byte[]? png,
        byte[]? m4a,
        ILogger? logger = null)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var safeCondition = Sanitize(conditionKey);
        var safeLanguage = Sanitize(language);
        var prefix = $"{stamp}_{safeCondition}_{safeLanguage}";
        Directory.CreateDirectory(RootDirectory);

        var saved = new List<string>(2);
        if (png is { Length: > 0 })
            saved.Add(Write(Path.Combine(RootDirectory, prefix + ".png"), png, logger));
        if (m4a is { Length: > 0 })
            saved.Add(Write(Path.Combine(RootDirectory, prefix + ".m4a"), m4a, logger));
        return saved;
    }

    private static string Write(string path, byte[] bytes, ILogger? logger)
    {
        File.WriteAllBytes(path, bytes);
        logger?.LogInformation("Saved weather media to {Path} ({Bytes} bytes)", path, bytes.Length);
        return path;
    }

    private static string Sanitize(string value)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0)
            return "unknown";

        var chars = text.Select(ch =>
            char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray();
        return new string(chars);
    }
}

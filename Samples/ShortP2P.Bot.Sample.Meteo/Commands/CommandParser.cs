namespace ShortP2P.Bot.Sample.Meteo.Commands;

internal static class CommandParser
{
    public static BotCommand Parse(string? text)
    {
        var trimmed = (text ?? "").Trim();
        if (trimmed.Length == 0)
            return new UnknownCommand("");

        var space = trimmed.IndexOf(' ');
        var head = (space < 0 ? trimmed : trimmed[..space]).ToLowerInvariant();
        var rest = space < 0 ? "" : trimmed[(space + 1)..].Trim();

        return head switch
        {
            "/start" => new StartCommand(),
            "/stop" => new StopCommand(),
            "/lang" => new LangCommand(rest),
            "/weather" => new WeatherCommand(rest),
            _ => new UnknownCommand(trimmed)
        };
    }
}

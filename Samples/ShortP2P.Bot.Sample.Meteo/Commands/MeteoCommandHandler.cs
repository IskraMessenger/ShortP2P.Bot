using ShortP2P.Bot.Sample.Meteo.Storage;
using ShortP2P.Bot.Sample.Meteo.Weather;

namespace ShortP2P.Bot.Sample.Meteo.Commands;

internal sealed class MeteoCommandHandler
{
    private readonly SqliteChatStore _chats;
    private readonly WeatherService _weather;
    private readonly ILogger<MeteoCommandHandler> _logger;

    public MeteoCommandHandler(SqliteChatStore chats, WeatherService weather, ILogger<MeteoCommandHandler> logger)
    {
        _chats = chats ?? throw new ArgumentNullException(nameof(chats));
        _weather = weather ?? throw new ArgumentNullException(nameof(weather));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> HandleAsync(string clientNetworkId, string text, CancellationToken cancellationToken)
    {
        var session = _chats.GetOrCreate(clientNetworkId);
        _chats.AddMessage(clientNetworkId, "in", text);
        var reply = await ReplyAsync(session, text, cancellationToken).ConfigureAwait(false);
        _chats.Save(session);
        _chats.AddMessage(clientNetworkId, "out", reply);
        return reply;
    }

    private async Task<string> ReplyAsync(ChatSession session, string text, CancellationToken cancellationToken)
    {
        var command = CommandParser.Parse(text);
        if (command is StartCommand)
        {
            session.IsActive = true;
            return MeteoTexts.Start(session.Language);
        }

        if (!session.IsActive)
            return MeteoTexts.NeedStart(session.Language);

        switch (command)
        {
            case StopCommand:
                session.IsActive = false;
                return MeteoTexts.Stopped(session.Language);
            case LangCommand lang:
                if (!MeteoTexts.TryNormalizeLanguage(lang.Code, out var language))
                    return MeteoTexts.UnknownLanguage(session.Language);
                session.Language = language;
                return MeteoTexts.LanguageChanged(language);
            case WeatherCommand weather:
                return await WeatherReplyAsync(session.Language, weather.Query, cancellationToken).ConfigureAwait(false);
            default:
                return MeteoTexts.Help(session.Language);
        }
    }

    private async Task<string> WeatherReplyAsync(string language, string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return MeteoTexts.WeatherUsage(language);

        if (LooksLikeCoordinates(query) && !GeoQuery.TryParseCoordinates(query, out _))
            return MeteoTexts.InvalidCoordinates(language);

        try
        {
            var report = await _weather.GetAsync(query, language, cancellationToken).ConfigureAwait(false);
            return MeteoTexts.FormatReport(
                language,
                report.DisplayName,
                report.ObservedAt,
                report.TemperatureC,
                report.HumidityPercent,
                report.WeatherCode);
        }
        catch (PlaceNotFoundException)
        {
            return MeteoTexts.PlaceNotFound(language);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weather request failed for {Query}", query);
            return MeteoTexts.WeatherUnavailable(language);
        }
    }

    private static bool LooksLikeCoordinates(string query)
    {
        return query.Any(char.IsDigit) && !query.Any(char.IsLetter);
    }
}

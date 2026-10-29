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

    public async Task<CommandResult> HandleAsync(string clientNetworkId, string text, CancellationToken cancellationToken)
    {
        var session = _chats.GetOrCreate(clientNetworkId);
        _chats.AddMessage(clientNetworkId, "in", text);
        var reply = await ReplyAsync(session, text, cancellationToken).ConfigureAwait(false);
        _chats.Save(session);
        _chats.AddMessage(clientNetworkId, "out", reply.Text);
        return reply;
    }

    private async Task<CommandResult> ReplyAsync(ChatSession session, string text, CancellationToken cancellationToken)
    {
        var command = CommandParser.Parse(text);
        if (command is StartCommand)
        {
            session.IsActive = true;
            return CommandResult.TextOnly(MeteoTexts.Start(session.Language), session.Language);
        }

        if (!session.IsActive)
            return CommandResult.TextOnly(MeteoTexts.NeedStart(session.Language), session.Language);

        switch (command)
        {
            case StopCommand:
                session.IsActive = false;
                return CommandResult.TextOnly(MeteoTexts.Stopped(session.Language), session.Language);
            case LangCommand lang:
                if (!MeteoTexts.TryNormalizeLanguage(lang.Code, out var language))
                    return CommandResult.TextOnly(MeteoTexts.UnknownLanguage(session.Language), session.Language);
                session.Language = language;
                return CommandResult.TextOnly(MeteoTexts.LanguageChanged(language), language);
            case WeatherCommand weather:
                return await WeatherReplyAsync(session.Language, weather.Query, cancellationToken).ConfigureAwait(false);
            default:
                return CommandResult.TextOnly(MeteoTexts.Help(session.Language), session.Language);
        }
    }

    private async Task<CommandResult> WeatherReplyAsync(string language, string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return CommandResult.TextOnly(MeteoTexts.WeatherUsage(language), language);

        if (LooksLikeCoordinates(query) && !GeoQuery.TryParseCoordinates(query, out _))
            return CommandResult.TextOnly(MeteoTexts.InvalidCoordinates(language), language);

        try
        {
            var report = await _weather.GetAsync(query, language, cancellationToken).ConfigureAwait(false);
            return new CommandResult
            {
                Language = language,
                Report = report,
                Text = MeteoTexts.FormatReport(
                    language,
                    report.DisplayName,
                    report.ObservedAt,
                    report.TemperatureC,
                    report.HumidityPercent,
                    report.WeatherCode)
            };
        }
        catch (PlaceNotFoundException)
        {
            return CommandResult.TextOnly(MeteoTexts.PlaceNotFound(language), language);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weather request failed for {Query}", query);
            return CommandResult.TextOnly(MeteoTexts.WeatherUnavailable(language), language);
        }
    }

    private static bool LooksLikeCoordinates(string query)
    {
        return query.Any(char.IsDigit) && !query.Any(char.IsLetter);
    }
}

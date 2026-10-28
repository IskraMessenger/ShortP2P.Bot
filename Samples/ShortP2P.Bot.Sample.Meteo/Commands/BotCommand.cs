namespace ShortP2P.Bot.Sample.Meteo.Commands;

internal abstract record BotCommand;

internal sealed record StartCommand : BotCommand;

internal sealed record StopCommand : BotCommand;

internal sealed record LangCommand(string Code) : BotCommand;

internal sealed record WeatherCommand(string Query) : BotCommand;

internal sealed record UnknownCommand(string Text) : BotCommand;

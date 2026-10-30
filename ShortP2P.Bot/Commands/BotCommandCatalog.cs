using System.Text.Json;
using Microsoft.Extensions.Options;
using ShortP2P.Bot.Contracts;
using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Commands;

/// <summary>
/// Loads <c>bot_commands</c> (JSON array of command, args, description) and checks incoming text against it.
/// </summary>
public sealed class BotCommandCatalog
{
    public const string DefaultFileName = "bot_commands";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly BotHostOptions _options;
    private readonly object _gate = new object();
    private IReadOnlyList<BotCommandDto>? _commands;

    public BotCommandCatalog(IOptions<BotHostOptions> options)
    {
        _options = options?.Value ?? new BotHostOptions();
    }

    public IReadOnlyList<BotCommandDto> Commands
    {
        get
        {
            lock (_gate)
            {
                _commands ??= Load();
                return _commands;
            }
        }
    }

    /// <summary>
    /// Plain text is not a command and is not validated.
    /// A <c>/Name</c> command is accepted only when it is listed and its values match <c>argType</c>.
    /// </summary>
    public BotCommandInspection Inspect(string? text)
    {
        if (!BotCommandSyntax.IsCommand(text))
        {
            return new BotCommandInspection
            {
                IsCommand = false,
                IsAccepted = false
            };
        }

        var trimmed = (text ?? "").Trim();
        var split = IndexOfWhitespace(trimmed);
        var head = split < 0 ? trimmed : trimmed.Substring(0, split);
        var rest = split < 0 ? "" : trimmed.Substring(split + 1).Trim();

        BotCommandDto? match = null;
        foreach (var command in Commands)
        {
            if (string.Equals(command.Command, head, StringComparison.OrdinalIgnoreCase))
            {
                match = command;
                break;
            }
        }

        if (match is null)
            return Reject("Unknown command.");

        if (!BotCommandArgumentParser.Matches(match.Args, rest))
            return Reject("Command arguments are invalid.");

        return new BotCommandInspection
        {
            IsCommand = true,
            IsAccepted = true
        };
    }

    private static BotCommandInspection Reject(string detail) =>
        new BotCommandInspection
        {
            IsCommand = true,
            IsAccepted = false,
            RejectionCode = BotCommandRejection.Code,
            RejectionMessage = BotCommandRejection.Message(detail)
        };

    private IReadOnlyList<BotCommandDto> Load()
    {
        var path = ResolvePath();
        if (!File.Exists(path))
        {
            if (!_options.RequireCommandsFile)
                return new List<BotCommandDto>();

            throw new InvalidOperationException($"Bot command file was not found: {path}");
        }

        List<BotCommandDto>? parsed;
        try
        {
            var json = File.ReadAllText(path);
            parsed = JsonSerializer.Deserialize<List<BotCommandDto>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Bot command file '{path}' is not a JSON array.", ex);
        }

        if (parsed is null)
            throw new InvalidOperationException($"Bot command file '{path}' must be a JSON array.");
        if (parsed.Count > BotLimits.MaxCommands)
            throw new InvalidOperationException($"Bot command file '{path}' lists more than {BotLimits.MaxCommands} commands.");

        var normalized = new List<BotCommandDto>(parsed.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in parsed)
        {
            if (!BotCommandRules.TryNormalize(source, out var command))
                throw new InvalidOperationException($"Bot command file '{path}' contains an invalid command.");
            if (!seen.Add(command.Command))
                throw new InvalidOperationException($"Bot command file '{path}' repeats '{command.Command}'.");
            normalized.Add(command);
        }

        return normalized;
    }

    private string ResolvePath()
    {
        var configured = _options.CommandsFile;
        var name = string.IsNullOrWhiteSpace(configured) ? DefaultFileName : configured.Trim();
        return Path.IsPathRooted(name) ? name : Path.Combine(AppContext.BaseDirectory, name);
    }

    private static int IndexOfWhitespace(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
                return i;
        }

        return -1;
    }
}

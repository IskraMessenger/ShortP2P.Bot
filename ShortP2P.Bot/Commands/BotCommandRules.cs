using System.Text.RegularExpressions;
using ShortP2P.Bot.Contracts;
using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Commands;

internal static class BotCommandRules
{
    private static readonly Regex CommandName = new Regex(
        "^/[A-Za-z][A-Za-z0-9_]{0,30}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryNormalize(BotCommandDto source, out BotCommandDto normalized)
    {
        normalized = new BotCommandDto();
        if (source is null)
            return false;

        var command = (source.Command ?? "").Trim();
        var description = (source.Description ?? "").Trim();
        if (!CommandName.IsMatch(command) || !IsCleartext(description, BotLimits.MaxCommandDescriptionLength))
            return false;
        if (!TryNormalizeArgs(source.Args, out var args))
            return false;

        normalized = new BotCommandDto
        {
            Command = command,
            Args = args,
            Description = description
        };
        return true;
    }

    private static bool TryNormalizeArgs(IReadOnlyList<BotCommandArgumentDto>? source, out List<BotCommandArgumentDto>? args)
    {
        args = null;
        if (source is null || source.Count == 0)
            return true;
        if (source.Count > BotLimits.MaxCommandArguments)
            return false;

        var normalized = new List<BotCommandArgumentDto>(source.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in source)
        {
            if (item is null)
                return false;

            var name = (item.ArgName ?? "").Trim();
            if (!IsArgumentName(name) || !seen.Add(name))
                return false;
            if (!BotCommandArgumentTypes.TryCanonical(item.ArgType, out var type))
                return false;

            normalized.Add(new BotCommandArgumentDto
            {
                ArgName = name,
                ArgType = type
            });
        }

        args = normalized;
        return true;
    }

    private static bool IsArgumentName(string name)
    {
        if (name.Length == 0 || name.Length > BotLimits.MaxCommandArgumentNameLength)
            return false;
        if (!IsLetter(name[0]))
            return false;

        for (var i = 1; i < name.Length; i++)
        {
            var ch = name[i];
            if (!IsLetter(ch) && !IsDigit(ch) && ch != '_')
                return false;
        }

        return true;
    }

    private static bool IsLetter(char ch) =>
        (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

    private static bool IsDigit(char ch) => ch >= '0' && ch <= '9';

    private static bool IsCleartext(string value, int maxLength)
    {
        if (value.Length == 0 || value.Length > maxLength)
            return false;

        foreach (var ch in value)
        {
            if (ch < (char)0x20 || ch == (char)0x7F || ch == '<' || ch == '>')
                return false;
        }

        return true;
    }
}

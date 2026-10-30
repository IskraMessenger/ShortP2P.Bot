using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Commands;

/// <summary>
/// Splits the text after a command into positional values and checks each one against <c>argType</c>.
/// The last <c>string</c>, <c>dynamic</c>, or <c>datetime</c> argument keeps the remainder of the line.
/// </summary>
internal static class BotCommandArgumentParser
{
    public static bool Matches(IReadOnlyList<BotCommandArgumentDto>? args, string rest)
    {
        if (args is null || args.Count == 0)
            return rest.Length == 0;

        var remaining = rest;
        for (var i = 0; i < args.Count; i++)
        {
            if (remaining.Length == 0)
                return false;

            var type = args[i].ArgType;
            var takesRemainder = i == args.Count - 1
                && (type == BotCommandArgumentTypes.String
                    || type == BotCommandArgumentTypes.Dynamic
                    || type == BotCommandArgumentTypes.DateTime);

            string token;
            if (takesRemainder)
            {
                token = remaining;
                remaining = "";
            }
            else
            {
                var split = IndexOfWhitespace(remaining);
                if (split < 0)
                {
                    token = remaining;
                    remaining = "";
                }
                else
                {
                    token = remaining.Substring(0, split);
                    remaining = remaining.Substring(split + 1).TrimStart();
                }
            }

            if (!BotCommandArgumentTypes.ValueMatches(type, token))
                return false;
        }

        return remaining.Length == 0;
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

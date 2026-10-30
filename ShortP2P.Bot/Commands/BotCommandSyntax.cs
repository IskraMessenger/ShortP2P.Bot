using System.Text.RegularExpressions;

namespace ShortP2P.Bot.Commands;

internal static class BotCommandSyntax
{
    /// <summary>A command is <c>/Name</c> with an optional argument tail. Plain text does not match.</summary>
    private static readonly Regex Command = new Regex(
        @"^/[A-Za-z][A-Za-z0-9_]*(\s|$)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsCommand(string? text)
    {
        var trimmed = (text ?? "").Trim();
        return trimmed.Length > 0 && Command.IsMatch(trimmed);
    }
}

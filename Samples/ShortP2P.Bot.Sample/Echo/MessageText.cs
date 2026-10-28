using System.Text;

namespace ShortP2P.Bot.Sample.Echo;

internal static class MessageText
{
    public static string Decode(string? encryptedMessageBase64)
    {
        if (string.IsNullOrWhiteSpace(encryptedMessageBase64))
            return "";

        try
        {
            var bytes = Convert.FromBase64String(encryptedMessageBase64.Trim());
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return encryptedMessageBase64;
        }
    }

    public static string Encode(string text) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    public static string ReplyTo(string original) =>
        $"Hello! You just typed: {original}";
}

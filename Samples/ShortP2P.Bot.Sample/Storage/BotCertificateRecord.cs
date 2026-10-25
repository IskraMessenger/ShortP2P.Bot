namespace ShortP2P.Bot.Sample.Storage;

internal sealed class BotCertificateRecord
{
    public required byte[] Pfx { get; init; }

    public required string FingerprintSha256 { get; init; }
}

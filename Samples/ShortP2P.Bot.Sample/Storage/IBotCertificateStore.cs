namespace ShortP2P.Bot.Sample.Storage;

internal interface IBotCertificateStore
{
    BotCertificateRecord GetOrCreate(string subjectCommonName);
}

namespace ShortP2P.Bot.Sample.Storage;

internal interface IRequestLog
{
    Task AddAsync(IncomingRequest request, CancellationToken cancellationToken = default);
}

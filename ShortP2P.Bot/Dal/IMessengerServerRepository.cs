using ShortP2P.Bot.Dal.Entities;

namespace ShortP2P.Bot.Dal;

/// <summary>
/// Persistence for the messenger server pool.
/// Implementations must target an RDBMS; this library does not depend on a concrete engine.
/// </summary>
public interface IMessengerServerRepository
{
    Task<MessengerServerEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<MessengerServerEntity?> GetByBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default);

    /// <summary>Available servers ordered for message ops: last successful first, then registration order.</summary>
    Task<IReadOnlyList<MessengerServerEntity>> GetAvailableOrderedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessengerServerEntity>> GetUnavailableAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessengerServerEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Insert or update by <see cref="MessengerServerEntity.BaseUrl"/>.</summary>
    Task UpsertAsync(MessengerServerEntity server, CancellationToken cancellationToken = default);

    Task MarkUnavailableAsync(long id, string? error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the server available again (clears last error; update last health-check timestamp).
    /// </summary>
    Task MarkAvailableAsync(long id, CancellationToken cancellationToken = default);

    Task MarkSuccessfulAsync(long id, DateTime successfulAtUtc, CancellationToken cancellationToken = default);

    Task UpdateHealthCheckAsync(long id, bool isAvailable, DateTime checkedAtUtc, string? error, CancellationToken cancellationToken = default);
}

using ShortP2P.Bot.Dal.Entities;

namespace ShortP2P.Bot.Dal;

/// <summary>Helpers for RDBMS-backed <see cref="IMessengerServerRepository"/> implementations.</summary>
public static class MessengerServerOrdering
{
    /// <summary>
    /// Last successful first, then earliest registered available server.
    /// </summary>
    public static IOrderedEnumerable<MessengerServerEntity> OrderForMessaging(
        IEnumerable<MessengerServerEntity> availableServers)
    {
        if (availableServers is null) throw new ArgumentNullException(nameof(availableServers));

        return availableServers
            .OrderByDescending(s => s.LastSuccessfulAtUtc.HasValue)
            .ThenByDescending(s => s.LastSuccessfulAtUtc)
            .ThenBy(s => s.RegisteredAtUtc)
            .ThenBy(s => s.Id);
    }
}

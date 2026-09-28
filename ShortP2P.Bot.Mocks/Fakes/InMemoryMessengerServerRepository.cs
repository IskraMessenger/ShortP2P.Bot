using ShortP2P.Bot.Dal;
using ShortP2P.Bot.Dal.Entities;

namespace ShortP2P.Bot.Mocks.Fakes;

public sealed class InMemoryMessengerServerRepository : IMessengerServerRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<long, MessengerServerEntity> _byId = new();
    private long _nextId = 1;

    public Task<MessengerServerEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            return Task.FromResult(_byId.TryGetValue(id, out var entity) ? Clone(entity) : null);
        }
    }

    public Task<MessengerServerEntity?> GetByBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var entity = _byId.Values.FirstOrDefault(s =>
                string.Equals(s.BaseUrl, baseUrl, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(entity is null ? null : Clone(entity));
        }
    }

    public Task<IReadOnlyList<MessengerServerEntity>> GetAvailableOrderedAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var list = MessengerServerOrdering
                .OrderForMessaging(_byId.Values.Where(s => s.IsAvailable))
                .Select(Clone)
                .ToArray();
            return Task.FromResult<IReadOnlyList<MessengerServerEntity>>(list);
        }
    }

    public Task<IReadOnlyList<MessengerServerEntity>> GetUnavailableAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var list = _byId.Values
                .Where(s => !s.IsAvailable)
                .OrderBy(s => s.Id)
                .Select(Clone)
                .ToArray();
            return Task.FromResult<IReadOnlyList<MessengerServerEntity>>(list);
        }
    }

    public Task<IReadOnlyList<MessengerServerEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var list = _byId.Values.OrderBy(s => s.Id).Select(Clone).ToArray();
            return Task.FromResult<IReadOnlyList<MessengerServerEntity>>(list);
        }
    }

    public Task UpsertAsync(MessengerServerEntity server, CancellationToken cancellationToken = default)
    {
        if (server is null) throw new ArgumentNullException(nameof(server));

        lock (_sync)
        {
            var existing = _byId.Values.FirstOrDefault(s =>
                string.Equals(s.BaseUrl, server.BaseUrl, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                if (server.Id == 0)
                    server.Id = _nextId++;

                _byId[server.Id] = Clone(server);
            }
            else
            {
                server.Id = existing.Id;
                _byId[existing.Id] = Clone(server);
            }
        }

        return Task.CompletedTask;
    }

    public Task MarkUnavailableAsync(long id, string? error, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_byId.TryGetValue(id, out var entity))
            {
                entity.IsAvailable = false;
                entity.LastError = error;
            }
        }

        return Task.CompletedTask;
    }

    public Task MarkAvailableAsync(long id, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_byId.TryGetValue(id, out var entity))
            {
                entity.IsAvailable = true;
                entity.LastError = null;
                entity.LastHealthCheckAtUtc = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    public Task MarkSuccessfulAsync(long id, DateTime successfulAtUtc, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_byId.TryGetValue(id, out var entity))
            {
                entity.LastSuccessfulAtUtc = successfulAtUtc;
                entity.IsAvailable = true;
                entity.LastError = null;
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateHealthCheckAsync(
        long id,
        bool isAvailable,
        DateTime checkedAtUtc,
        string? error,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_byId.TryGetValue(id, out var entity))
            {
                entity.IsAvailable = isAvailable;
                entity.LastHealthCheckAtUtc = checkedAtUtc;
                entity.LastError = error;
            }
        }

        return Task.CompletedTask;
    }

    private static MessengerServerEntity Clone(MessengerServerEntity source) => new()
    {
        Id = source.Id,
        BaseUrl = source.BaseUrl,
        NetworkId = source.NetworkId,
        BotKey = source.BotKey,
        IsAvailable = source.IsAvailable,
        LastSuccessfulAtUtc = source.LastSuccessfulAtUtc,
        RegisteredAtUtc = source.RegisteredAtUtc,
        LastHealthCheckAtUtc = source.LastHealthCheckAtUtc,
        LastError = source.LastError
    };
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.Http;

namespace ShortP2P.Bot.Jobs;

/// <summary>
/// Periodically pings previously unavailable servers via GET /api/v1/health/ping
/// and restores them to the available pool when the ping succeeds.
/// </summary>
public sealed class UnavailableServersHealthCheckJob : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnavailableServersHealthCheckJob> _logger;
    private readonly TimeSpan _interval;

    public UnavailableServersHealthCheckJob(
        IServiceScopeFactory scopeFactory,
        ILogger<UnavailableServersHealthCheckJob> logger)
        : this(scopeFactory, logger, DefaultInterval)
    {
    }

    public UnavailableServersHealthCheckJob(
        IServiceScopeFactory scopeFactory,
        ILogger<UnavailableServersHealthCheckJob> logger,
        TimeSpan interval)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _interval = interval <= TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(interval))
            : interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Unavailable servers health check job started (interval {Interval})",
            _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unavailable servers health check cycle failed");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var servers = scope.ServiceProvider.GetRequiredService<IMessengerServerRepository>();
        var api = scope.ServiceProvider.GetRequiredService<IMessengerServerApiClient>();

        var unavailableServers = await servers.GetUnavailableAsync(cancellationToken).ConfigureAwait(false);
        if (unavailableServers.Count == 0)
            return;

        _logger.LogDebug("Health-checking {Count} unavailable server(s)", unavailableServers.Count);

        foreach (var server in unavailableServers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var checkedAt = DateTime.UtcNow;

            try
            {
                await api.PingAsync(new Uri(server.BaseUrl, UriKind.Absolute), cancellationToken)
                    .ConfigureAwait(false);

                await servers.MarkAvailableAsync(server.Id, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Server {BaseUrl} is available again", server.BaseUrl);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await servers.UpdateHealthCheckAsync(server.Id, isAvailable: false, checkedAt, ex.Message, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug(ex, "Server {BaseUrl} is still unavailable", server.BaseUrl);
            }
        }
    }
}

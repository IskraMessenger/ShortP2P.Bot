using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ShortP2P.Bot.Dal.Entities;
using ShortP2P.Bot.Jobs;
using ShortP2P.Bot.Mocks.Mocks;

namespace ShortP2P.Bot.Tests;

public sealed class UnavailableServersHealthCheckJobTests
{
    [Fact]
    public async Task RunOnce_MarksServerAvailable_WhenPingSucceeds()
    {
        using var mock = MessengerServerMock.Start().SetupPingOk();
        var (services, servers) = BotTestHost.Create();
        await using (services)
        {
            await servers.UpsertAsync(new MessengerServerEntity
            {
                BaseUrl = mock.BaseUri.ToString().TrimEnd('/'),
                NetworkId = "botNet",
                BotName = "echo",
                BotKey = MessengerServerMock.DefaultBotKey,
                ServerFingerprintSha256 = MessengerServerMock.DefaultServerFingerprint,
                IsAvailable = false,
                LastError = "down",
                RegisteredAtUtc = DateTime.UtcNow
            });

            var job = new UnavailableServersHealthCheckJob(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<UnavailableServersHealthCheckJob>.Instance,
                UnavailableServersHealthCheckJob.DefaultInterval);

            await job.RunOnceAsync(CancellationToken.None);

            var entity = Assert.Single(await servers.GetAllAsync());
            Assert.True(entity.IsAvailable);
            Assert.Null(entity.LastError);
            Assert.NotNull(entity.LastHealthCheckAtUtc);
        }
    }

    [Fact]
    public async Task RunOnce_KeepsServerUnavailable_WhenPingFails()
    {
        using var mock = MessengerServerMock.Start().SetupPingUnavailable();
        var (services, servers) = BotTestHost.Create();
        await using (services)
        {
            await servers.UpsertAsync(new MessengerServerEntity
            {
                BaseUrl = mock.BaseUri.ToString().TrimEnd('/'),
                NetworkId = "bot_network_id",
                BotName = "echo",
                BotKey = MessengerServerMock.DefaultBotKey,
                ServerFingerprintSha256 = MessengerServerMock.DefaultServerFingerprint,
                IsAvailable = false,
                LastError = "down",
                RegisteredAtUtc = DateTime.UtcNow
            });

            var job = new UnavailableServersHealthCheckJob(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<UnavailableServersHealthCheckJob>.Instance,
                UnavailableServersHealthCheckJob.DefaultInterval);

            await job.RunOnceAsync(CancellationToken.None);

            var entity = Assert.Single(await servers.GetAllAsync());
            Assert.False(entity.IsAvailable);
            Assert.False(string.IsNullOrEmpty(entity.LastError));
        }
    }

    [Fact]
    public async Task RunOnce_DoesNotRestore_BlockedServer()
    {
        using var mock = MessengerServerMock.Start().SetupPingOk();
        var (services, servers) = BotTestHost.Create();
        await using (services)
        {
            await servers.UpsertAsync(new MessengerServerEntity
            {
                BaseUrl = mock.BaseUri.ToString().TrimEnd('/'),
                NetworkId = "botNet",
                BotName = "echo",
                BotKey = MessengerServerMock.DefaultBotKey,
                ServerFingerprintSha256 = MessengerServerMock.DefaultServerFingerprint,
                IsBlocked = true,
                IsAvailable = false,
                LastError = "fingerprint mismatch",
                RegisteredAtUtc = DateTime.UtcNow
            });

            var job = new UnavailableServersHealthCheckJob(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<UnavailableServersHealthCheckJob>.Instance,
                UnavailableServersHealthCheckJob.DefaultInterval);

            await job.RunOnceAsync(CancellationToken.None);

            var entity = Assert.Single(await servers.GetAllAsync());
            Assert.True(entity.IsBlocked);
            Assert.False(entity.IsAvailable);
        }
    }
}

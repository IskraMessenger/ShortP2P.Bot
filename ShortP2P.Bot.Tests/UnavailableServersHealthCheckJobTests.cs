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
                BotKey = MessengerServerMock.DefaultBotKey,
                IsAvailable = false,
                LastError = "down",
                RegisteredAtUtc = DateTime.UtcNow
            });

            var job = new UnavailableServersHealthCheckJob(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<UnavailableServersHealthCheckJob>.Instance,
                TimeSpan.FromMinutes(3));

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
                NetworkId = "botNet",
                BotKey = MessengerServerMock.DefaultBotKey,
                IsAvailable = false,
                LastError = "down",
                RegisteredAtUtc = DateTime.UtcNow
            });

            var job = new UnavailableServersHealthCheckJob(
                services.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<UnavailableServersHealthCheckJob>.Instance,
                TimeSpan.FromMinutes(3));

            await job.RunOnceAsync(CancellationToken.None);

            var entity = Assert.Single(await servers.GetAllAsync());
            Assert.False(entity.IsAvailable);
            Assert.False(string.IsNullOrEmpty(entity.LastError));
        }
    }
}

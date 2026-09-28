using ShortP2P.Bot.Contracts.Dtos;
using ShortP2P.Bot.Mocks.Fakes;
using ShortP2P.Bot.Mocks.Mocks;

namespace ShortP2P.Bot.Tests;

public sealed class BotMessengerTests
{
    [Fact]
    public async Task Register_AddsServerToPool()
    {
        using var mock = MessengerServerMock.Start().SetupRegister();
        var (services, servers) = BotTestHost.Create();

        await using (services)
        {
            var response = await services.Messenger().RegisterAsync(mock.BaseUri, new BotRegisterRequest
            {
                NetworkId = "botNet",
                BotName = "echo",
                BotReadableName = "Echo",
                BotDescription = "d"
            });

            var pool = await servers.GetAllAsync();
            var entity = Assert.Single(pool);
            Assert.Equal(mock.BaseUri.ToString().TrimEnd('/'), entity.BaseUrl);
            Assert.Equal(response.BotKey, entity.BotKey);
            Assert.True(entity.IsAvailable);
        }
    }

    [Fact]
    public async Task Register_DoesNotExceedMaxServers()
    {
        using var extra = MessengerServerMock.Start().SetupRegister();
        var (services, servers) = BotTestHost.Create();

        await using (services)
        {
            for (var i = 0; i < BotPoolLimits.MaxServers; i++)
            {
                await servers.UpsertAsync(new Dal.Entities.MessengerServerEntity
                {
                    BaseUrl = $"http://server-{i}.local",
                    NetworkId = $"net-{i}",
                    BotKey = MessengerServerMock.DefaultBotKey,
                    IsAvailable = true,
                    RegisteredAtUtc = DateTime.UtcNow
                });
            }

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                services.Messenger().RegisterAsync(extra.BaseUri, new BotRegisterRequest
                {
                    NetworkId = "botNet",
                    BotName = "echo",
                    BotReadableName = "Echo",
                    BotDescription = "d"
                }));

            Assert.Contains(BotPoolLimits.MaxServers.ToString(), ex.Message);
            Assert.Equal(BotPoolLimits.MaxServers, (await servers.GetAllAsync()).Count);
        }
    }

    [Fact]
    public async Task SendMessagesViaPool_UsesLastSuccessfulThenFallsBack()
    {
        using var down = MessengerServerMock.Start().SetupSendMessagesUnavailable();
        using var up = MessengerServerMock.Start().SetupSendMessages();
        var (services, servers) = BotTestHost.Create();
        await using (services)
        {
            await SeedPoolAsync(servers, down.BaseUri, lastSuccessful: DateTime.UtcNow);
            await SeedPoolAsync(servers, up.BaseUri, lastSuccessful: null);

            var response = await services.Messenger().SendMessagesViaPoolAsync(
                "req-pool",
                [
                    new BotClientMessageDto
                    {
                        NetworkId = "client1",
                        EncryptedMessageBase64 = "ZW5j",
                        CorrelationId = "c1"
                    }
                ]);

            Assert.Equal("req-pool", response.RequestId);

            var downEntity = await servers.GetByBaseUrlAsync(down.BaseUri.ToString().TrimEnd('/'));
            var upEntity = await servers.GetByBaseUrlAsync(up.BaseUri.ToString().TrimEnd('/'));
            Assert.False(downEntity!.IsAvailable);
            Assert.True(upEntity!.IsAvailable);
            Assert.NotNull(upEntity.LastSuccessfulAtUtc);
        }
    }

    [Fact]
    public async Task WaitForIncomeMessagesViaPool_ReturnsMessagesFromAvailableServer()
    {
        using var mock = MessengerServerMock.Start().SetupWaitForIncomeMessages(
        [
            new BotClientMessageDto
            {
                NetworkId = "client1",
                EncryptedMessageBase64 = "ZW5j",
                CorrelationId = "in-1"
            }
        ]);
        var (services, servers) = BotTestHost.Create();
        await using (services)
        {
            await SeedPoolAsync(servers, mock.BaseUri);

            var response = await services.Messenger().WaitForIncomeMessagesViaPoolAsync("req-in", timeoutSeconds: 1);

            var message = Assert.Single(response.Messages);
            Assert.Equal("in-1", message.CorrelationId);
        }
    }

    [Fact]
    public async Task SendMessagesViaPool_Throws_WhenPoolIsEmpty()
    {
        var (services, _) = BotTestHost.Create();
        await using (services)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                services.Messenger().SendMessagesViaPoolAsync("req", Array.Empty<BotClientMessageDto>()));
        }
    }

    private static Task SeedPoolAsync(
        InMemoryMessengerServerRepository servers,
        Uri baseUri,
        DateTime? lastSuccessful = null)
        => servers.UpsertAsync(new Dal.Entities.MessengerServerEntity
        {
            BaseUrl = baseUri.ToString().TrimEnd('/'),
            NetworkId = "botNet",
            BotKey = MessengerServerMock.DefaultBotKey,
            IsAvailable = true,
            RegisteredAtUtc = DateTime.UtcNow,
            LastSuccessfulAtUtc = lastSuccessful
        });
}

using ShortP2P.Bot.Contracts.Dtos;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Mocks.Fakes;
using ShortP2P.Bot.Mocks.Mocks;
using ShortP2P.Bot.Security;

namespace ShortP2P.Bot.Tests;

public sealed class BotMessengerTests
{
    [Fact]
    public async Task Register_AddsServerToPool_WithPinnedFingerprint()
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
                BotDescription = "d",
                MessagePublicKey = "{\"M\":\"AQAB\",\"E\":\"AQAB\"}"
            });

            var pool = await servers.GetAllAsync();
            var entity = Assert.Single(pool);
            Assert.Equal(mock.BaseUri.ToString().TrimEnd('/'), entity.BaseUrl);
            Assert.Equal(response.BotKey, entity.BotKey);
            Assert.Equal("echo", entity.BotName);
            Assert.Equal(MessengerServerMock.DefaultServerFingerprint, entity.ServerFingerprintSha256);
            Assert.Equal(MessengerServerMock.DefaultServerFingerprint, response.ServerFingerprintSha256);
            Assert.False(entity.IsBlocked);
            Assert.True(entity.IsAvailable);

            var registerLog = mock.Server.LogEntries.Single(e =>
                e.RequestMessage.Path.Contains("register", StringComparison.Ordinal));
            Assert.Contains(
                services.Certificate().FingerprintSha256,
                registerLog.RequestMessage.Body,
                StringComparison.OrdinalIgnoreCase);
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
                    BotName = $"bot-{i}",
                    BotKey = MessengerServerMock.DefaultBotKey,
                    ServerFingerprintSha256 = MessengerServerMock.DefaultServerFingerprint,
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
    public async Task Login_Succeeds_WhenServerFingerprintMatchesPin()
    {
        using var mock = MessengerServerMock.Start()
            .SetupRegister()
            .SetupLogin();
        var (services, servers) = BotTestHost.Create();

        await using (services)
        {
            await services.Messenger().RegisterAsync(mock.BaseUri, new BotRegisterRequest
            {
                NetworkId = "botNet",
                BotName = "echo",
                BotReadableName = "Echo",
                BotDescription = "d"
            });

            var login = await services.Messenger().LoginAsync(mock.BaseUri);

            Assert.Equal(MessengerServerMock.DefaultToken, login.Token);
            var entity = Assert.Single(await servers.GetAllAsync());
            Assert.False(entity.IsBlocked);
        }
    }

    [Fact]
    public async Task Login_BlocksServer_WhenServerFingerprintMismatches()
    {
        const string otherFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        using var mock = MessengerServerMock.Start()
            .SetupRegister(serverFingerprintSha256: MessengerServerMock.DefaultServerFingerprint)
            .SetupLogin(serverFingerprintSha256: otherFingerprint);
        var (services, servers) = BotTestHost.Create();

        await using (services)
        {
            await services.Messenger().RegisterAsync(mock.BaseUri, new BotRegisterRequest
            {
                NetworkId = "botNet",
                BotName = "echo",
                BotReadableName = "Echo",
                BotDescription = "d"
            });

            var ex = await Assert.ThrowsAsync<MessengerServerCertificateException>(
                () => services.Messenger().LoginAsync(mock.BaseUri));

            Assert.Equal(MessengerServerMock.DefaultServerFingerprint, ex.ExpectedFingerprint);
            Assert.Equal(otherFingerprint, ex.ActualFingerprint);

            var entity = Assert.Single(await servers.GetAllAsync());
            Assert.True(entity.IsBlocked);
            Assert.False(entity.IsAvailable);
            Assert.Empty(await servers.GetAvailableOrderedAsync());
        }
    }

    [Fact]
    public async Task SendMessages_Throws_WhenServerIsBlocked()
    {
        using var mock = MessengerServerMock.Start().SetupSendMessages();
        var (services, servers) = BotTestHost.Create();

        await using (services)
        {
            await servers.UpsertAsync(new Dal.Entities.MessengerServerEntity
            {
                BaseUrl = mock.BaseUri.ToString().TrimEnd('/'),
                NetworkId = "botNet",
                BotName = "echo",
                BotKey = MessengerServerMock.DefaultBotKey,
                ServerFingerprintSha256 = MessengerServerMock.DefaultServerFingerprint,
                IsBlocked = true,
                IsAvailable = false,
                RegisteredAtUtc = DateTime.UtcNow,
                LastError = "blocked"
            });

            await Assert.ThrowsAsync<MessengerServerCertificateException>(() =>
                services.Messenger().SendMessagesAsync(mock.BaseUri, new BotSendMessagesRequest
                {
                    RequestId = "req",
                    BotNetworkId = "botNet",
                    BotKey = MessengerServerMock.DefaultBotKey,
                    Messages = Array.Empty<BotClientMessageDto>()
                }));
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
            BotName = "echo",
            BotKey = MessengerServerMock.DefaultBotKey,
            ServerFingerprintSha256 = MessengerServerMock.DefaultServerFingerprint,
            IsAvailable = true,
            RegisteredAtUtc = DateTime.UtcNow,
            LastSuccessfulAtUtc = lastSuccessful
        });
}

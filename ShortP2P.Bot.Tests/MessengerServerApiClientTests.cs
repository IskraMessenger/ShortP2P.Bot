using ShortP2P.Bot.Contracts.Dtos;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Mocks.Mocks;

namespace ShortP2P.Bot.Tests;

public sealed class MessengerServerApiClientTests
{
    [Fact]
    public async Task Ping_Succeeds_When_HealthEndpointReturnsOk()
    {
        using var mock = MessengerServerMock.Start().SetupPingOk();
        await using var host = BotTestHost.Create().Services;

        await host.Api().PingAsync(mock.BaseUri);
    }

    [Fact]
    public async Task Ping_Throws_When_ServerUnavailable()
    {
        using var mock = MessengerServerMock.Start().SetupPingUnavailable();
        await using var host = BotTestHost.Create().Services;

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(
            () => host.Api().PingAsync(mock.BaseUri));

        Assert.True(ex.IndicatesServerUnavailable);
    }

    [Fact]
    public async Task Register_ReturnsIssuedBotKey()
    {
        using var mock = MessengerServerMock.Start().SetupRegister();
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().RegisterAsync(mock.BaseUri, new BotRegisterRequest
        {
            NetworkId = "botNet",
            BotName = "echo",
            BotReadableName = "Echo Bot",
            BotDescription = "test"
        });

        Assert.Equal("botNet", response.NetworkId);
        Assert.Equal("echo", response.BotName);
        Assert.Equal(MessengerServerMock.DefaultBotKey, response.BotKey);
    }

    [Fact]
    public async Task Login_ReturnsToken()
    {
        using var mock = MessengerServerMock.Start().SetupLogin();
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().LoginAsync(mock.BaseUri, new BotLoginRequest
        {
            NetworkId = "botNet",
            BotName = "echo",
            BotKey = MessengerServerMock.DefaultBotKey
        });

        Assert.Equal(MessengerServerMock.DefaultToken, response.Token);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task SendMessages_EchoesRequestId_AndPreservesCorrelationIdOnWire()
    {
        using var mock = MessengerServerMock.Start().SetupSendMessages();
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().SendMessagesAsync(mock.BaseUri, new BotSendMessagesRequest
        {
            RequestId = "req-1",
            BotNetworkId = "botNet",
            BotKey = MessengerServerMock.DefaultBotKey,
            Messages =
            [
                new BotClientMessageDto
                {
                    NetworkId = "client1",
                    EncryptedMessageBase64 = "ZW5j",
                    CorrelationId = "corr-42"
                }
            ]
        });

        Assert.Equal("req-1", response.RequestId);

        var log = mock.Server.LogEntries.Single(e =>
            e.RequestMessage.Path.Contains("send_messages", StringComparison.Ordinal));
        Assert.Contains("corr-42", log.RequestMessage.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WaitForIncomeMessages_ReturnsInboxWithCorrelationId()
    {
        using var mock = MessengerServerMock.Start().SetupWaitForIncomeMessages(
        [
            new BotClientMessageDto
            {
                NetworkId = "client1",
                EncryptedMessageBase64 = "ZW5j",
                CorrelationId = "corr-in"
            }
        ]);
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().WaitForIncomeMessagesAsync(mock.BaseUri, new BotWaitForIncomeMessagesRequest
        {
            RequestId = "req-wait",
            BotNetworkId = "botNet",
            BotKey = MessengerServerMock.DefaultBotKey,
            TimeoutSeconds = 1
        });

        Assert.Equal("req-wait", response.RequestId);
        var message = Assert.Single(response.Messages);
        Assert.Equal("client1", message.NetworkId);
        Assert.Equal("corr-in", message.CorrelationId);
    }
}

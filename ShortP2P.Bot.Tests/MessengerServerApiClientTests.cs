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
    public async Task GetServerCertificate_ReturnsFingerprint()
    {
        using var mock = MessengerServerMock.Start().SetupServerCertificate();
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().GetServerCertificateAsync(mock.BaseUri);

        Assert.Equal(MessengerServerMock.DefaultServerFingerprint, response.FingerprintSha256);
    }

    [Fact]
    public async Task Register_ReturnsIssuedBotKey_AndServerFingerprint()
    {
        using var mock = MessengerServerMock.Start().SetupRegister();
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().RegisterAsync(mock.BaseUri, new BotRegisterRequest
        {
            NetworkId = "botNet",
            BotName = "echo",
            BotReadableName = "Echo Bot",
            BotDescription = "test",
            FingerprintSha256 = new string('c', 64)
        });

        Assert.Equal("botNet", response.NetworkId);
        Assert.Equal("echo", response.BotName);
        Assert.Equal(MessengerServerMock.DefaultBotKey, response.BotKey);
        Assert.Equal(MessengerServerMock.DefaultServerFingerprint, response.ServerFingerprintSha256);
    }

    [Fact]
    public async Task Login_ReturnsToken_AndServerFingerprint()
    {
        using var mock = MessengerServerMock.Start().SetupLogin();
        await using var host = BotTestHost.Create().Services;

        var response = await host.Api().LoginAsync(mock.BaseUri, new BotLoginRequest
        {
            NetworkId = "botNet",
            BotName = "echo",
            BotKey = MessengerServerMock.DefaultBotKey,
            FingerprintSha256 = new string('c', 64)
        });

        Assert.Equal(MessengerServerMock.DefaultToken, response.Token);
        Assert.True(response.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(MessengerServerMock.DefaultServerFingerprint, response.ServerFingerprintSha256);
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
        Assert.Contains("bot_server_interaction", log.RequestMessage.Path, StringComparison.Ordinal);
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

    [Fact]
    public async Task SubmitReceipt_Succeeds()
    {
        using var mock = MessengerServerMock.Start().SetupReceipts();
        await using var host = BotTestHost.Create().Services;

        await host.Api().SubmitReceiptAsync(mock.BaseUri, new BotServerReceiptRequest
        {
            RequestId = "req-receipt",
            BotNetworkId = "botNet",
            BotKey = MessengerServerMock.DefaultBotKey,
            MessageId = "m1"
        });
    }

    [Fact]
    public async Task PutBlobChunk_And_GetManifest_And_GetChunk_RoundTrip()
    {
        using var mock = MessengerServerMock.Start()
            .SetupPutBlobChunk()
            .SetupGetBlobChunkManifest()
            .SetupGetBlobChunk(ciphertext: new byte[] { 9, 8, 7 });
        await using var host = BotTestHost.Create().Services;

        await host.Api().PutBlobChunkAsync(
            mock.BaseUri,
            MessengerServerMock.DefaultToken,
            "m1",
            chunkIndex: 0,
            targetNetworkId: "client1",
            totalChunks: 1,
            totalBytes: 3,
            ciphertext: new byte[] { 9, 8, 7 });

        var manifest = await host.Api().GetBlobChunkManifestAsync(
            mock.BaseUri,
            MessengerServerMock.DefaultToken,
            "m1");

        Assert.Equal("m1", manifest.BlobId);
        Assert.Equal(new[] { 0 }, manifest.Indexes);

        var chunk = await host.Api().GetBlobChunkAsync(
            mock.BaseUri,
            MessengerServerMock.DefaultToken,
            "m1",
            chunkIndex: 0);

        Assert.Equal(new byte[] { 9, 8, 7 }, chunk);
    }

    [Fact]
    public async Task PutBlobChunk_Throws_When_InsufficientStorage()
    {
        using var mock = MessengerServerMock.Start().SetupPutBlobChunkInsufficientStorage();
        await using var host = BotTestHost.Create().Services;

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            host.Api().PutBlobChunkAsync(
                mock.BaseUri,
                MessengerServerMock.DefaultToken,
                "m1",
                chunkIndex: 0,
                targetNetworkId: "client1",
                totalChunks: 1,
                totalBytes: 999,
                ciphertext: new byte[] { 1 }));

        Assert.True(ex.IndicatesInsufficientStorage);
        Assert.Contains(ApiError.InsufficientStorage, ex.ResponseBody, StringComparison.Ordinal);
    }
}

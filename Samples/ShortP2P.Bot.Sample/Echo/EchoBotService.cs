using Microsoft.Extensions.Options;
using ShortP2P.Bot.Contracts.Dtos;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.Messaging;
using ShortP2P.Bot.Sample.Configuration;
using ShortP2P.Bot.Sample.Storage;

namespace ShortP2P.Bot.Sample.Echo;

internal sealed class EchoBotService : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IBotMessenger _messenger;
    private readonly IMessengerServerRepository _servers;
    private readonly IRequestLog _requests;
    private readonly IBotCertificateStore _certificates;
    private readonly SqliteMessageKeyStore _messageKeys;
    private readonly SampleBotOptions _options;
    private readonly ILogger<EchoBotService> _logger;
    private string _privateKeyJson = "";

    public EchoBotService(
        IBotMessenger messenger,
        IMessengerServerRepository servers,
        IRequestLog requests,
        IBotCertificateStore certificates,
        SqliteMessageKeyStore messageKeys,
        IOptions<SampleBotOptions> options,
        ILogger<EchoBotService> logger)
    {
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _servers = servers ?? throw new ArgumentNullException(nameof(servers));
        _requests = requests ?? throw new ArgumentNullException(nameof(requests));
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _messageKeys = messageKeys ?? throw new ArgumentNullException(nameof(messageKeys));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sample bot started");
        try
        {
            var serverUri = RequireServerUri();
            RequireBotIdentity();
            var certificate = _certificates.GetOrCreate(_options.Bot.Name);
            var keys = EnsureMessageKeys();
            _privateKeyJson = keys.PrivateJson;

            if (!await EnsureRegisteredAsync(serverUri, certificate.FingerprintSha256, keys.PublicJson, stoppingToken)
                    .ConfigureAwait(false))
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollOnceAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Echo cycle failed");
                    await DelayAsync(stoppingToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sample bot failed");
            throw;
        }
        finally
        {
            _logger.LogInformation("Sample bot stopped");
        }
    }

    private (string PublicJson, string PrivateJson) EnsureMessageKeys()
    {
        var existing = _messageKeys.TryGet();
        if (existing is not null)
        {
            _logger.LogInformation("Using stored RSA message key pair");
            return existing.Value;
        }

        var created = BotMessageCrypto.CreateRsaKeyPairJson();
        _messageKeys.Save(created.PublicJson, created.PrivateJson);
        _logger.LogInformation("Generated new RSA message key pair for client→bot encryption");
        return created;
    }

    private async Task<bool> EnsureRegisteredAsync(
        Uri serverUri,
        string fingerprintSha256,
        string messagePublicKey,
        CancellationToken stoppingToken)
    {
        var baseUrl = serverUri.ToString().TrimEnd('/');
        var bot = _options.Bot;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var existing = await _servers.GetByBaseUrlAsync(baseUrl, stoppingToken).ConfigureAwait(false);
                if (existing is not null && !string.IsNullOrEmpty(existing.BotKey))
                {
                    // Old registrations had no MessagePublicKey → empty GET /bots catalog.
                    // Remove + re-register so the server stores the RSA public key.
                    _logger.LogInformation(
                        "Re-registering on {BaseUrl} to publish MessagePublicKey",
                        baseUrl);
                    try
                    {
                        await _messenger.RemoveAsync(
                            serverUri,
                            new BotRemoveRequest
                            {
                                NetworkId = existing.NetworkId,
                                BotKey = existing.BotKey
                            },
                            stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Remove before re-register on {BaseUrl} failed; trying register anyway", baseUrl);
                    }
                }

                await _messenger.RegisterAsync(serverUri, new BotRegisterRequest
                {
                    NetworkId = bot.NetworkId.Trim(),
                    BotName = bot.Name.Trim(),
                    BotReadableName = bot.ReadableName.Trim(),
                    BotDescription = bot.Description.Trim(),
                    FingerprintSha256 = fingerprintSha256,
                    MessagePublicKey = messagePublicKey
                }, stoppingToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Registered on {BaseUrl} with MessagePublicKey (len={KeyLen})",
                    baseUrl,
                    messagePublicKey.Length);
                return true;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration on {BaseUrl} failed", baseUrl);
                await DelayAsync(stoppingToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    private async Task PollOnceAsync(CancellationToken stoppingToken)
    {
        var income = await _messenger.WaitForIncomeMessagesViaPoolAsync(
            NewRequestId(),
            _options.MessengerServer.LongPollTimeoutSeconds,
            stoppingToken).ConfigureAwait(false);

        if (income.Messages.Count == 0)
            return;

        var replies = new List<BotClientMessageDto>(income.Messages.Count);
        foreach (var message in income.Messages)
        {
            string text;
            try
            {
                text = BotMessageCrypto.DecryptEnvelopeToText(
                    message.EncryptedMessageBase64 ?? "",
                    _privateKeyJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decrypt failed; falling back to plain base64 decode");
                text = MessageText.Decode(message.EncryptedMessageBase64);
            }

            var reply = MessageText.ReplyTo(text);
            _logger.LogInformation(
                "Received message from {ClientNetworkId}: {Text}",
                message.NetworkId,
                text);

            await _requests.AddAsync(new IncomingRequest
            {
                ReceivedAtUtc = DateTime.UtcNow,
                ClientNetworkId = message.NetworkId,
                CorrelationId = message.CorrelationId,
                MessageText = text,
                EncryptedMessageBase64 = message.EncryptedMessageBase64 ?? "",
                ReplyText = reply
            }, stoppingToken).ConfigureAwait(false);

            var clientPub = message.ClientMessagePublicKey?.Trim() ?? "";
            if (clientPub.Length == 0)
            {
                _logger.LogError(
                    "No ClientMessagePublicKey for {ClientNetworkId} — client must PublishChatRequest to the bot first; skipping reply",
                    message.NetworkId);
                continue;
            }

            string encryptedReply;
            try
            {
                encryptedReply = BotMessageCrypto.EncryptTextToEnvelopeBase64(reply, clientPub);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt reply for {ClientNetworkId}", message.NetworkId);
                continue;
            }

            replies.Add(new BotClientMessageDto
            {
                NetworkId = message.NetworkId,
                EncryptedMessageBase64 = encryptedReply,
                CorrelationId = message.CorrelationId
            });

            _logger.LogInformation(
                "Prepared encrypted reply to {ClientNetworkId}: {Reply}",
                message.NetworkId,
                reply);
        }

        if (replies.Count == 0)
            return;

        await _messenger.SendMessagesViaPoolAsync(NewRequestId(), replies, stoppingToken).ConfigureAwait(false);

        foreach (var reply in replies)
        {
            _logger.LogInformation(
                "Sent reply to {ClientNetworkId} (cipherLen={Len})",
                reply.NetworkId,
                reply.EncryptedMessageBase64?.Length ?? 0);
        }
    }

    private Uri RequireServerUri()
    {
        var baseUrl = _options.MessengerServer.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("MessengerServer:BaseUrl must be an absolute URL.");

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("MessengerServer:BaseUrl must use https.");

        return uri;
    }

    private void RequireBotIdentity()
    {
        var bot = _options.Bot;
        if (string.IsNullOrWhiteSpace(bot.NetworkId)
            || string.IsNullOrWhiteSpace(bot.Name)
            || string.IsNullOrWhiteSpace(bot.ReadableName)
            || string.IsNullOrWhiteSpace(bot.Description))
        {
            throw new InvalidOperationException(
                "Bot:NetworkId, Name, ReadableName and Description are required.");
        }
    }

    private async Task DelayAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(RetryDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private static string NewRequestId() => Guid.NewGuid().ToString("N");
}

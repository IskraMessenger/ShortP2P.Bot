using ShortP2P.Bot.Contracts.Dtos;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Messaging;
using ShortP2P.Bot.Sample.Echo;

namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>
/// Encrypts a weather PNG and voice file for the client, uploads ciphertext in 50 KB chunks,
/// and returns transfer-offer messages the client uses to download those chunks.
/// </summary>
internal sealed class WeatherAttachmentSender
{
    private readonly IBotMessenger _messenger;
    private readonly IMessengerServerRepository _servers;
    private readonly IMessengerServerApiClient _api;
    private readonly ILogger<WeatherAttachmentSender> _logger;

    public WeatherAttachmentSender(
        IBotMessenger messenger,
        IMessengerServerRepository servers,
        IMessengerServerApiClient api,
        ILogger<WeatherAttachmentSender> logger)
    {
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        _servers = servers ?? throw new ArgumentNullException(nameof(servers));
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<BotClientMessageDto>> UploadAsync(
        string clientNetworkId,
        string clientPublicKeyJson,
        string? correlationId,
        byte[]? png,
        byte[]? m4a,
        CancellationToken cancellationToken)
    {
        if ((png is null || png.Length == 0) && (m4a is null || m4a.Length == 0))
            return Array.Empty<BotClientMessageDto>();

        var servers = await _servers.GetAvailableOrderedAsync(cancellationToken).ConfigureAwait(false);
        var server = servers.FirstOrDefault(item => !item.IsBlocked && !string.IsNullOrWhiteSpace(item.BotKey));
        if (server is null)
            throw new InvalidOperationException("No messenger server is available for blob upload.");

        var serverUri = new Uri(server.BaseUrl, UriKind.Absolute);
        var login = await _messenger.LoginAsync(serverUri, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(login.Token))
            throw new InvalidOperationException("Messenger server login did not return a token.");

        var host = server.BaseUrl.TrimEnd('/');
        var expires = DateTime.UtcNow.AddHours(24).Ticks;
        var messages = new List<BotClientMessageDto>(2);
        if (png is { Length: > 0 })
        {
            messages.Add(await UploadOneAsync(
                serverUri,
                login.Token,
                host,
                expires,
                clientNetworkId,
                clientPublicKeyJson,
                correlationId,
                "image",
                "weather.png",
                "image/png",
                png,
                cancellationToken).ConfigureAwait(false));
        }

        if (m4a is { Length: > 0 })
        {
            messages.Add(await UploadOneAsync(
                serverUri,
                login.Token,
                host,
                expires,
                clientNetworkId,
                clientPublicKeyJson,
                correlationId,
                "audio",
                "weather.m4a",
                "audio/mp4",
                m4a,
                cancellationToken).ConfigureAwait(false));
        }

        return messages;
    }

    private async Task<BotClientMessageDto> UploadOneAsync(
        Uri serverUri,
        string bearerToken,
        string host,
        long expiresUtcTicks,
        string clientNetworkId,
        string clientPublicKeyJson,
        string? correlationId,
        string payloadKind,
        string fileName,
        string mimeType,
        byte[] plaintext,
        CancellationToken cancellationToken)
    {
        var inner = payloadKind.Equals("image", StringComparison.OrdinalIgnoreCase)
            ? BotMessageCrypto.EncodeChatWireImage(mimeType, plaintext)
            : BotMessageCrypto.EncodeChatWireFile(fileName, mimeType, plaintext);
        var ciphertext = BotMessageCrypto.EncryptEnvelope(inner, clientPublicKeyJson);
        var chunks = BlobChunks.Split(ciphertext);
        var sha = BotMessageCrypto.Sha256Hex(ciphertext);
        var blobId = Guid.NewGuid().ToString("N");

        for (var i = 0; i < chunks.Length; i++)
        {
            await _api.PutBlobChunkAsync(
                serverUri,
                bearerToken,
                blobId,
                i,
                clientNetworkId,
                chunks.Length,
                ciphertext.Length,
                chunks[i],
                cancellationToken).ConfigureAwait(false);
        }

        var offer = BotMessageCrypto.EncodeChatWireTransferOffer(new ChatWireTransferOffer(
            blobId,
            sha,
            payloadKind,
            fileName,
            mimeType,
            plaintext.LongLength,
            host,
            0,
            expiresUtcTicks,
            blobId,
            chunks.Length,
            sha));

        _logger.LogInformation(
            "Uploaded {PayloadKind} blob {BlobId}: plaintext {PlainBytes} bytes, ciphertext {CipherBytes} bytes, {Chunks} chunks",
            payloadKind,
            blobId,
            plaintext.Length,
            ciphertext.Length,
            chunks.Length);

        return new BotClientMessageDto
        {
            NetworkId = clientNetworkId,
            EncryptedMessageBase64 = Convert.ToBase64String(BotMessageCrypto.EncryptEnvelope(offer, clientPublicKeyJson)),
            CorrelationId = correlationId
        };
    }
}

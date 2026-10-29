using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Http;

/// <summary>HTTPS calls against a single messenger server base URL.</summary>
public interface IMessengerServerApiClient
{
    Task PingAsync(Uri serverBaseUrl, CancellationToken cancellationToken = default);

    Task<ServerCertificateResponse> GetServerCertificateAsync(
        Uri serverBaseUrl,
        CancellationToken cancellationToken = default);

    Task<BotRegisterResponse> RegisterAsync(
        Uri serverBaseUrl,
        BotRegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<BotLoginResponse> LoginAsync(
        Uri serverBaseUrl,
        BotLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<BotRemoveResponse> RemoveAsync(
        Uri serverBaseUrl,
        BotRemoveRequest request,
        CancellationToken cancellationToken = default);

    Task<BotSendMessagesResponse> SendMessagesAsync(
        Uri serverBaseUrl,
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default);

    Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        Uri serverBaseUrl,
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>POST bot receipt; server drops the client → bot message and its chunks.</summary>
    Task SubmitReceiptAsync(
        Uri serverBaseUrl,
        BotServerReceiptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>PUT one ciphertext chunk (JWT required). Query: targetNetworkId, totalChunks, totalBytes.</summary>
    Task PutBlobChunkAsync(
        Uri serverBaseUrl,
        string bearerToken,
        string blobId,
        int chunkIndex,
        string targetNetworkId,
        int totalChunks,
        long totalBytes,
        byte[] ciphertext,
        CancellationToken cancellationToken = default);

    /// <summary>GET which chunk indexes are already stored (JWT required).</summary>
    Task<BlobChunkManifestDto> GetBlobChunkManifestAsync(
        Uri serverBaseUrl,
        string bearerToken,
        string blobId,
        CancellationToken cancellationToken = default);

    /// <summary>GET one ciphertext chunk (JWT required).</summary>
    Task<byte[]> GetBlobChunkAsync(
        Uri serverBaseUrl,
        string bearerToken,
        string blobId,
        int chunkIndex,
        CancellationToken cancellationToken = default);
}

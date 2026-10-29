using System.Net.Http.Json;
using System.Text.Json;
using ShortP2P.Bot.Contracts;
using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Http;

public sealed class MessengerServerApiClient : IMessengerServerApiClient
{
    /// <summary>Default timeout for regular HTTPS calls (ping, register, login, send).</summary>
    public static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(20);

    /// <summary>Default long-poll wait when <see cref="BotWaitForIncomeMessagesRequest.TimeoutSeconds"/> is omitted.</summary>
    public const int DefaultLongPollTimeoutSeconds = 30;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public MessengerServerApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task PingAsync(Uri serverBaseUrl, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Get,
            ServerApiRoutes.HealthPing,
            content: null,
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ServerCertificateResponse> GetServerCertificateAsync(
        Uri serverBaseUrl,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Get,
            ServerApiRoutes.ServerCertificate,
            content: null,
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<ServerCertificateResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BotRegisterResponse> RegisterAsync(
        Uri serverBaseUrl,
        BotRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Post,
            ServerApiRoutes.BotRegister,
            JsonContent.Create(request, options: JsonOptions),
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<BotRegisterResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BotLoginResponse> LoginAsync(
        Uri serverBaseUrl,
        BotLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Post,
            ServerApiRoutes.BotLogin,
            JsonContent.Create(request, options: JsonOptions),
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<BotLoginResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BotRemoveResponse> RemoveAsync(
        Uri serverBaseUrl,
        BotRemoveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Post,
            ServerApiRoutes.BotRemove,
            JsonContent.Create(request, options: JsonOptions),
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<BotRemoveResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BotSendMessagesResponse> SendMessagesAsync(
        Uri serverBaseUrl,
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Post,
            ServerApiRoutes.BotSendMessages,
            JsonContent.Create(request, options: JsonOptions),
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<BotSendMessagesResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        Uri serverBaseUrl,
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var timeoutSeconds = request.TimeoutSeconds ?? DefaultLongPollTimeoutSeconds;
        var httpTimeout = TimeSpan.FromSeconds(Math.Max(35, timeoutSeconds + 10));

        var payload = new BotWaitForIncomeMessagesRequest
        {
            RequestId = request.RequestId,
            BotNetworkId = request.BotNetworkId,
            BotKey = request.BotKey,
            TimeoutSeconds = timeoutSeconds
        };

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Post,
            ServerApiRoutes.BotWaitForIncomeMessages,
            JsonContent.Create(payload, options: JsonOptions),
            httpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<BotWaitForIncomeMessagesResponse>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task SubmitReceiptAsync(
        Uri serverBaseUrl,
        BotServerReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Post,
            ServerApiRoutes.BotReceipts,
            JsonContent.Create(request, options: JsonOptions),
            HttpTimeout,
            cancellationToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task PutBlobChunkAsync(
        Uri serverBaseUrl,
        string bearerToken,
        string blobId,
        int chunkIndex,
        string targetNetworkId,
        int totalChunks,
        long totalBytes,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            throw new ArgumentException("Bearer token is required.", nameof(bearerToken));
        if (string.IsNullOrWhiteSpace(blobId))
            throw new ArgumentException("Blob id is required.", nameof(blobId));
        if (string.IsNullOrWhiteSpace(targetNetworkId))
            throw new ArgumentException("Target network id is required.", nameof(targetNetworkId));
        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        if (ciphertext is null)
            throw new ArgumentNullException(nameof(ciphertext));

        var relativePath =
            $"{BlobLimits.BlobChunk(blobId, chunkIndex)}" +
            $"?targetNetworkId={Uri.EscapeDataString(targetNetworkId.Trim())}" +
            $"&totalChunks={totalChunks}&totalBytes={totalBytes}";

        using var content = new ByteArrayContent(ciphertext);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Put,
            relativePath,
            content,
            HttpTimeout,
            cancellationToken,
            bearerToken,
            BlobLimits.TargetNetworkIdHeader,
            targetNetworkId.Trim()).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BlobChunkManifestDto> GetBlobChunkManifestAsync(
        Uri serverBaseUrl,
        string bearerToken,
        string blobId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            throw new ArgumentException("Bearer token is required.", nameof(bearerToken));
        if (string.IsNullOrWhiteSpace(blobId))
            throw new ArgumentException("Blob id is required.", nameof(blobId));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Get,
            BlobLimits.BlobChunks(blobId),
            content: null,
            HttpTimeout,
            cancellationToken,
            bearerToken).ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadRequiredAsync<BlobChunkManifestDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> GetBlobChunkAsync(
        Uri serverBaseUrl,
        string bearerToken,
        string blobId,
        int chunkIndex,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            throw new ArgumentException("Bearer token is required.", nameof(bearerToken));
        if (string.IsNullOrWhiteSpace(blobId))
            throw new ArgumentException("Blob id is required.", nameof(blobId));
        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));

        using var response = await SendAsync(
            serverBaseUrl,
            HttpMethod.Get,
            BlobLimits.BlobChunk(blobId, chunkIndex),
            content: null,
            HttpTimeout,
            cancellationToken,
            bearerToken,
            accept: "application/octet-stream").ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Uri serverBaseUrl,
        HttpMethod method,
        string relativePath,
        HttpContent? content,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        string? bearerToken = null,
        string? extraHeaderName = null,
        string? extraHeaderValue = null,
        string? accept = null)
    {
        if (serverBaseUrl is null) throw new ArgumentNullException(nameof(serverBaseUrl));
        if (!serverBaseUrl.IsAbsoluteUri)
            throw new ArgumentException("Server base URL must be absolute.", nameof(serverBaseUrl));

        var client = _httpClientFactory.CreateClient(HttpClientNames.MessengerServer);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        var requestUri = new Uri(NormalizeBaseUrl(serverBaseUrl), relativePath.TrimStart('/'));
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + bearerToken.Trim());

        if (!string.IsNullOrWhiteSpace(extraHeaderName) && extraHeaderValue is not null)
            request.Headers.TryAddWithoutValidation(extraHeaderName, extraHeaderValue);

        if (!string.IsNullOrWhiteSpace(accept))
            request.Headers.TryAddWithoutValidation("Accept", accept);

        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MessengerServerApiException($"Request to '{requestUri}' timed out.");
        }
        catch (HttpRequestException ex)
        {
            throw new MessengerServerApiException($"Request to '{requestUri}' failed: {ex.Message}");
        }
    }

    private static Uri NormalizeBaseUrl(Uri serverBaseUrl)
    {
        var text = serverBaseUrl.ToString().TrimEnd('/') + "/";
        return new Uri(text, UriKind.Absolute);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string? body = null;
        try
        {
            body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore body read failures
        }

        throw new MessengerServerApiException(
            $"HTTP {(int)response.StatusCode} from '{response.RequestMessage?.RequestUri}'.",
            response.StatusCode,
            body);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (value is null)
            throw new MessengerServerApiException($"Empty JSON body from '{response.RequestMessage?.RequestUri}'.");

        return value;
    }
}

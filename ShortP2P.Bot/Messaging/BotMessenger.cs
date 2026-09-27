using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ShortP2P.Bot.Contracts.Dtos;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.Dal.Entities;
using ShortP2P.Bot.Http;

namespace ShortP2P.Bot.Messaging;

public sealed class BotMessenger : IBotMessenger
{
    private const string AllServersUnavailableMessage = "ALL SERVERS ARE UNAVAILABLE!";

    private static readonly TimeSpan[] PoolRetryDelays =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3)
    };

    private readonly IMessengerServerApiClient _apiClient;
    private readonly IMessengerServerRepository _servers;
    private readonly ILogger<BotMessenger> _logger;
    private readonly AsyncRetryPolicy _poolRetryPolicy;

    public BotMessenger(
        IMessengerServerApiClient apiClient,
        IMessengerServerRepository servers,
        ILogger<BotMessenger> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _servers = servers ?? throw new ArgumentNullException(nameof(servers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _poolRetryPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                PoolRetryDelays,
                (exception, delay, retryAttempt, _) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Server pool pass failed (retry {RetryAttempt} after {Delay})",
                        retryAttempt,
                        delay);
                });
    }

    public async Task<BotRegisterResponse> RegisterAsync(
        Uri serverBaseUrl,
        BotRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var baseUrl = NormalizeBaseUrl(serverBaseUrl);
        var existing = await _servers.GetByBaseUrlAsync(baseUrl, cancellationToken).ConfigureAwait(false);

        if (existing is null)
        {
            var all = await _servers.GetAllAsync(cancellationToken).ConfigureAwait(false);
            if (all.Count >= BotPoolLimits.MaxServers)
            {
                throw new InvalidOperationException(
                    $"Messenger server pool limit reached ({BotPoolLimits.MaxServers}). " +
                    "Remove a server before registering on a new one.");
            }
        }

        var response = await _apiClient.RegisterAsync(serverBaseUrl, request, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var entity = existing ?? new MessengerServerEntity
        {
            RegisteredAtUtc = now
        };

        entity.BaseUrl = baseUrl;
        entity.NetworkId = response.NetworkId;
        entity.BotKey = response.BotKey;
        entity.IsAvailable = true;
        entity.LastError = null;
        if (existing is null)
            entity.RegisteredAtUtc = now;

        await _servers.UpsertAsync(entity, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Bot registered on {BaseUrl}; server added/updated in pool", baseUrl);
        return response;
    }

    public async Task<BotSendMessagesResponse> SendMessagesAsync(
        Uri serverBaseUrl,
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        _logger.LogDebug(
            "SendMessages enter: server={Server}, requestId={RequestId}, messages={MessageCount}",
            serverBaseUrl,
            request.RequestId,
            request.Messages?.Count ?? 0);

        try
        {
            var response = await _apiClient.SendMessagesAsync(serverBaseUrl, request, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "SendMessages exit: server={Server}, requestId={RequestId}",
                serverBaseUrl,
                response.RequestId);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessages failed: server={Server}, requestId={RequestId}", serverBaseUrl, request.RequestId);
            await TryMarkServerUnavailableByUrlAsync(serverBaseUrl, ex, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        Uri serverBaseUrl,
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        _logger.LogDebug(
            "WaitForIncomeMessages enter: server={Server}, requestId={RequestId}, timeoutSeconds={TimeoutSeconds}",
            serverBaseUrl,
            request.RequestId,
            request.TimeoutSeconds);

        try
        {
            var response = await _apiClient.WaitForIncomeMessagesAsync(serverBaseUrl, request, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "WaitForIncomeMessages exit: server={Server}, requestId={RequestId}, messages={MessageCount}",
                serverBaseUrl,
                response.RequestId,
                response.Messages?.Count ?? 0);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WaitForIncomeMessages failed: server={Server}, requestId={RequestId}",
                serverBaseUrl,
                request.RequestId);
            await TryMarkServerUnavailableByUrlAsync(serverBaseUrl, ex, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<BotSendMessagesResponse> SendMessagesViaPoolAsync(
        string requestId,
        IReadOnlyList<BotClientMessageDto> messages,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id is required.", nameof(requestId));
        if (messages is null)
            throw new ArgumentNullException(nameof(messages));

        _logger.LogDebug(
            "SendMessagesViaPool enter: requestId={RequestId}, messages={MessageCount}",
            requestId,
            messages.Count);

        try
        {
            var response = await _poolRetryPolicy.ExecuteAsync(
                ct => ExecuteAgainstPoolAsync(
                    async (server, token) =>
                    {
                        var request = new BotSendMessagesRequest
                        {
                            RequestId = requestId,
                            BotNetworkId = server.NetworkId,
                            BotKey = server.BotKey,
                            Messages = messages
                        };

                        return await _apiClient
                            .SendMessagesAsync(new Uri(server.BaseUrl, UriKind.Absolute), request, token)
                            .ConfigureAwait(false);
                    },
                    operationName: "SendMessages",
                    ct),
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "SendMessagesViaPool exit: requestId={RequestId}",
                response.RequestId);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessagesViaPool failed: requestId={RequestId}", requestId);
            throw;
        }
    }

    public async Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesViaPoolAsync(
        string requestId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request id is required.", nameof(requestId));

        _logger.LogDebug(
            "WaitForIncomeMessagesViaPool enter: requestId={RequestId}, timeoutSeconds={TimeoutSeconds}",
            requestId,
            timeoutSeconds);

        try
        {
            var response = await _poolRetryPolicy.ExecuteAsync(
                ct => ExecuteAgainstPoolAsync(
                    async (server, token) =>
                    {
                        var request = new BotWaitForIncomeMessagesRequest
                        {
                            RequestId = requestId,
                            BotNetworkId = server.NetworkId,
                            BotKey = server.BotKey,
                            TimeoutSeconds = timeoutSeconds
                        };

                        return await _apiClient
                            .WaitForIncomeMessagesAsync(new Uri(server.BaseUrl, UriKind.Absolute), request, token)
                            .ConfigureAwait(false);
                    },
                    operationName: "WaitForIncomeMessages",
                    ct),
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "WaitForIncomeMessagesViaPool exit: requestId={RequestId}, messages={MessageCount}",
                response.RequestId,
                response.Messages?.Count ?? 0);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WaitForIncomeMessagesViaPool failed: requestId={RequestId}", requestId);
            throw;
        }
    }

    private async Task<T> ExecuteAgainstPoolAsync<T>(
        Func<MessengerServerEntity, CancellationToken, Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        var servers = await _servers.GetAvailableOrderedAsync(cancellationToken).ConfigureAwait(false);
        if (servers.Count == 0)
        {
            _logger.LogInformation(AllServersUnavailableMessage);
            throw new InvalidOperationException("Messenger server pool is empty or has no available servers.");
        }

        Exception? lastError = null;

        foreach (var server in servers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await operation(server, cancellationToken).ConfigureAwait(false);
                await _servers.MarkSuccessfulAsync(server.Id, DateTime.UtcNow, cancellationToken)
                    .ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogError(
                    ex,
                    "{Operation} failed on server {BaseUrl} (id={ServerId})",
                    operationName,
                    server.BaseUrl,
                    server.Id);

                if (ShouldMarkUnavailable(ex))
                {
                    await _servers.MarkUnavailableAsync(server.Id, ex.Message, cancellationToken)
                        .ConfigureAwait(false);
                    _logger.LogInformation(
                        "Server unavailable: {BaseUrl} (id={ServerId})",
                        server.BaseUrl,
                        server.Id);
                }
            }
        }

        _logger.LogInformation(AllServersUnavailableMessage);
        throw new AggregateException(
            "All available messenger servers failed for this pool pass.",
            lastError is null ? Array.Empty<Exception>() : new[] { lastError });
    }

    private async Task TryMarkServerUnavailableByUrlAsync(
        Uri serverBaseUrl,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (!ShouldMarkUnavailable(exception))
            return;

        var baseUrl = NormalizeBaseUrl(serverBaseUrl);
        var server = await _servers.GetByBaseUrlAsync(baseUrl, cancellationToken).ConfigureAwait(false);
        if (server is null)
            return;

        await _servers.MarkUnavailableAsync(server.Id, exception.Message, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Server unavailable: {BaseUrl} (id={ServerId})", server.BaseUrl, server.Id);
    }

    private static bool ShouldMarkUnavailable(Exception exception)
    {
        return exception switch
        {
            MessengerServerApiException api => api.IndicatesServerUnavailable,
            HttpRequestException => true,
            TaskCanceledException => true,
            _ => false
        };
    }

    private static string NormalizeBaseUrl(Uri serverBaseUrl)
    {
        if (serverBaseUrl is null) throw new ArgumentNullException(nameof(serverBaseUrl));
        if (!serverBaseUrl.IsAbsoluteUri)
            throw new ArgumentException("Server base URL must be absolute.", nameof(serverBaseUrl));

        return serverBaseUrl.ToString().TrimEnd('/');
    }
}

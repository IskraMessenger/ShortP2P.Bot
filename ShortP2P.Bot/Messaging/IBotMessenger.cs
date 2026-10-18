using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Messaging;

/// <summary>
/// Bot ↔ messenger-server messaging: single-server and whole-pool operations.
/// Pool is used for send/receive; registration and login always target an explicit server URL.
/// </summary>
public interface IBotMessenger
{
    /// <summary>
    /// Registers the bot on <paramref name="serverBaseUrl"/> (sends the bot certificate fingerprint),
    /// pins the server certificate fingerprint from the response, and upserts the server into the local pool.
    /// </summary>
    Task<BotRegisterResponse> RegisterAsync(
        Uri serverBaseUrl,
        BotRegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes the bot on a previously registered server and verifies the server certificate fingerprint
    /// against the pin stored at registration. On mismatch the server is blocked for further operations.
    /// </summary>
    Task<BotLoginResponse> LoginAsync(
        Uri serverBaseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Send messages to a specific server (credentials must be provided in the request).</summary>
    Task<BotSendMessagesResponse> SendMessagesAsync(
        Uri serverBaseUrl,
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Long-poll income messages on a specific server.</summary>
    Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        Uri serverBaseUrl,
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send via the available server pool (last successful → remaining available),
    /// with Polly retries: up to 3 full passes, delays 1s then 3s between passes.
    /// Blocked servers are excluded.
    /// </summary>
    Task<BotSendMessagesResponse> SendMessagesViaPoolAsync(
        string requestId,
        IReadOnlyList<BotClientMessageDto> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive via the available server pool with the same ordering and Polly retry policy as send.
    /// Blocked servers are excluded.
    /// </summary>
    Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesViaPoolAsync(
        string requestId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);
}

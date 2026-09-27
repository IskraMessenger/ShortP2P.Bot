using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Messaging;

/// <summary>
/// Bot ↔ messenger-server messaging: single-server and whole-pool operations.
/// Pool is used for send/receive; registration always targets an explicit server URL.
/// </summary>
public interface IBotMessenger
{
    /// <summary>
    /// Registers the bot on <paramref name="serverBaseUrl"/> and upserts the server into the local pool on success.
    /// </summary>
    Task<BotRegisterResponse> RegisterAsync(
        Uri serverBaseUrl,
        BotRegisterRequest request,
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
    /// </summary>
    Task<BotSendMessagesResponse> SendMessagesViaPoolAsync(
        string requestId,
        IReadOnlyList<BotClientMessageDto> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Receive via the available server pool with the same ordering and Polly retry policy as send.
    /// </summary>
    Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesViaPoolAsync(
        string requestId,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default);
}

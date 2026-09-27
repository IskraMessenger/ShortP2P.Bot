using ShortP2P.Bot.Contracts.Dtos;

namespace ShortP2P.Bot.Http;

/// <summary>HTTPS calls against a single messenger server base URL.</summary>
public interface IMessengerServerApiClient
{
    Task PingAsync(Uri serverBaseUrl, CancellationToken cancellationToken = default);

    Task<BotRegisterResponse> RegisterAsync(
        Uri serverBaseUrl,
        BotRegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<BotLoginResponse> LoginAsync(
        Uri serverBaseUrl,
        BotLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<BotSendMessagesResponse> SendMessagesAsync(
        Uri serverBaseUrl,
        BotSendMessagesRequest request,
        CancellationToken cancellationToken = default);

    Task<BotWaitForIncomeMessagesResponse> WaitForIncomeMessagesAsync(
        Uri serverBaseUrl,
        BotWaitForIncomeMessagesRequest request,
        CancellationToken cancellationToken = default);
}

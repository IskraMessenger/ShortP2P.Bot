using System.Net;

namespace ShortP2P.Bot.Http;

public sealed class MessengerServerApiException(
    string message,
    HttpStatusCode? statusCode = null,
    string? responseBody = null) : Exception(message)
{
    private HttpStatusCode? StatusCode { get; } = statusCode;

    public string? ResponseBody { get; } = responseBody;

    /// <summary>True when the failure indicates the host is likely unreachable / unhealthy.</summary>
    public bool IndicatesServerUnavailable =>
        StatusCode is null
        || (int)StatusCode.Value >= 500
        || StatusCode == HttpStatusCode.RequestTimeout
        || StatusCode == HttpStatusCode.ServiceUnavailable
        || StatusCode == HttpStatusCode.GatewayTimeout;

    /// <summary>True when the server returned HTTP 507 (bot chunk upload disk budget exceeded).</summary>
    public bool IndicatesInsufficientStorage =>
        StatusCode == HttpStatusCode.InsufficientStorage;
}

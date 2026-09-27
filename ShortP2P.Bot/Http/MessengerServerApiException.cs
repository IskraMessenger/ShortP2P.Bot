using System.Net;

namespace ShortP2P.Bot.Http;

public sealed class MessengerServerApiException : Exception
{
    public MessengerServerApiException(string message, HttpStatusCode? statusCode = null, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode? StatusCode { get; }

    public string? ResponseBody { get; }

    /// <summary>True when the failure indicates the host is likely unreachable / unhealthy.</summary>
    public bool IndicatesServerUnavailable =>
        StatusCode is null
        || (int)StatusCode.Value >= 500
        || StatusCode == HttpStatusCode.RequestTimeout
        || StatusCode == HttpStatusCode.ServiceUnavailable
        || StatusCode == HttpStatusCode.GatewayTimeout;
}

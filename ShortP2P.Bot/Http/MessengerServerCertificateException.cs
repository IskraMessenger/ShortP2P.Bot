namespace ShortP2P.Bot.Http;

/// <summary>
/// Raised when the messenger server TLS fingerprint does not match the value pinned at registration.
/// Further operations against that server must be blocked.
/// </summary>
public sealed class MessengerServerCertificateException : Exception
{
    public MessengerServerCertificateException(
        string message,
        string? expectedFingerprint = null,
        string? actualFingerprint = null)
        : base(message)
    {
        ExpectedFingerprint = expectedFingerprint;
        ActualFingerprint = actualFingerprint;
    }

    public string? ExpectedFingerprint { get; }

    public string? ActualFingerprint { get; }
}

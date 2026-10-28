using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using ShortP2P.Bot.Sample.Configuration;
using ShortP2P.Bot.Security;

namespace ShortP2P.Bot.Sample.Storage;

/// <summary>
/// Adapts the sample SQLite certificate store to the library <see cref="IBotCertificateProvider"/>.
/// Keeps register/login fingerprints aligned with the cert used by the host.
/// </summary>
internal sealed class StoreBackedBotCertificateProvider : IBotCertificateProvider
{
    private readonly IBotCertificateStore _store;
    private readonly SampleBotOptions _options;
    private readonly object _sync = new();
    private BotCertificateRecord? _record;

    public StoreBackedBotCertificateProvider(
        IBotCertificateStore store,
        IOptions<SampleBotOptions> options)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public string FingerprintSha256 => EnsureLoaded().FingerprintSha256;

    public X509Certificate2 GetCertificate()
    {
        var record = EnsureLoaded();
        return new X509Certificate2(record.Pfx);
    }

    private BotCertificateRecord EnsureLoaded()
    {
        if (_record is not null)
            return _record;

        lock (_sync)
        {
            return _record ??= _store.GetOrCreate(_options.Bot.Name);
        }
    }
}

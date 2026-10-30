using Microsoft.Extensions.DependencyInjection;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Jobs;
using ShortP2P.Bot.Messaging;
using ShortP2P.Bot.Security;

namespace ShortP2P.Bot.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers HTTPS messenger client, bot certificate provider, pool messenger and unavailable-servers health job.
    /// The host must also register <see cref="Dal.IMessengerServerRepository"/>.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configureCertificate">
    /// Optional certificate settings: set <see cref="BotCertificateOptions.PfxPath"/> to use an existing PFX,
    /// or leave it empty to auto-generate (and reuse) a self-signed certificate.
    /// </param>
    public static IServiceCollection AddShortP2PBot(
        this IServiceCollection services,
        Action<BotCertificateOptions>? configureCertificate = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var optionsBuilder = services.AddOptions<BotCertificateOptions>();
        if (configureCertificate is not null)
            optionsBuilder.Configure(configureCertificate);

        services.AddOptions<BotHostOptions>();

        services.AddSingleton<IBotCertificateProvider, BotCertificateProvider>();

        services.AddHttpClient(HttpClientNames.MessengerServer, client =>
        {
            // Per-request CancelAfter enforces timeouts; long-poll needs more than HttpTimeout.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddTransient<IMessengerServerApiClient, MessengerServerApiClient>();
        services.AddTransient<IBotMessenger, BotMessenger>();
        services.AddHostedService<UnavailableServersHealthCheckJob>();

        return services;
    }
}

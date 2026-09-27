using Microsoft.Extensions.DependencyInjection;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Jobs;
using ShortP2P.Bot.Messaging;

namespace ShortP2P.Bot.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers HTTPS messenger client, pool messenger and unavailable-servers health job.
    /// The host must also register <see cref="Dal.IMessengerServerRepository"/>.
    /// </summary>
    public static IServiceCollection AddShortP2PBot(this IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

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

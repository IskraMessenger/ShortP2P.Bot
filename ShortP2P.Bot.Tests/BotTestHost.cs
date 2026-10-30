using Microsoft.Extensions.DependencyInjection;
using ShortP2P.Bot;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.DependencyInjection;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Messaging;
using ShortP2P.Bot.Mocks.Fakes;
using ShortP2P.Bot.Security;

namespace ShortP2P.Bot.Tests;

internal static class BotTestHost
{
    public static (ServiceProvider Services, InMemoryMessengerServerRepository Servers) Create(
        Action<BotCertificateOptions>? configureCertificate = null)
    {
        var servers = new InMemoryMessengerServerRepository();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessengerServerRepository>(servers);
        services.AddShortP2PBot(options =>
        {
            // Keep auto-generated PFX out of the repo / shared build output during tests.
            options.AutoGeneratePfxPath = Path.Combine(
                Path.GetTempPath(),
                "ShortP2P.Bot.Tests",
                Guid.NewGuid().ToString("N"),
                "bot.pfx");
            configureCertificate?.Invoke(options);
        });
        services.Configure<BotHostOptions>(options => options.RequireCommandsFile = false);
        return (services.BuildServiceProvider(), servers);
    }

    public static IMessengerServerApiClient Api(this ServiceProvider services) =>
        services.GetRequiredService<IMessengerServerApiClient>();

    public static IBotMessenger Messenger(this ServiceProvider services) =>
        services.GetRequiredService<IBotMessenger>();

    public static IBotCertificateProvider Certificate(this ServiceProvider services) =>
        services.GetRequiredService<IBotCertificateProvider>();
}

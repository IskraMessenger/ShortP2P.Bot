using Microsoft.Extensions.DependencyInjection;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.DependencyInjection;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Messaging;
using ShortP2P.Bot.Mocks.Fakes;

namespace ShortP2P.Bot.Tests;

internal static class BotTestHost
{
    public static (ServiceProvider Services, InMemoryMessengerServerRepository Servers) Create()
    {
        var servers = new InMemoryMessengerServerRepository();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessengerServerRepository>(servers);
        services.AddShortP2PBot();
        return (services.BuildServiceProvider(), servers);
    }

    public static IMessengerServerApiClient Api(this ServiceProvider services) =>
        services.GetRequiredService<IMessengerServerApiClient>();

    public static IBotMessenger Messenger(this ServiceProvider services) =>
        services.GetRequiredService<IBotMessenger>();
}

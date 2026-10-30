using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ShortP2P.Bot;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.DependencyInjection;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Sample.Configuration;
using ShortP2P.Bot.Sample.Echo;
using ShortP2P.Bot.Sample.Storage;
using ShortP2P.Bot.Security;

LogManager.Configuration = new XmlLoggingConfiguration(Path.Combine(AppContext.BaseDirectory, "nlog.config"));

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddNLog();

builder.Services.Configure<SampleBotOptions>(builder.Configuration);
builder.Services.AddSingleton<SampleBotDatabase>();
builder.Services.AddSingleton<IMessengerServerRepository, SqliteMessengerServerRepository>();
builder.Services.AddSingleton<IRequestLog, SqliteRequestLog>();
builder.Services.AddSingleton<IBotCertificateStore, SqliteBotCertificateStore>();
builder.Services.AddSingleton<SqliteMessageKeyStore>();
builder.Services.AddShortP2PBot();
builder.Services.Configure<BotHostOptions>(options =>
    options.Version = builder.Configuration.GetValue("Bot:Version", 1));
// Prefer the sample SQLite-backed cert over the library auto-generated PFX.
builder.Services.AddSingleton<IBotCertificateProvider, StoreBackedBotCertificateProvider>();

if (builder.Configuration.GetValue("MessengerServer:AllowInvalidCertificate", false))
{
    builder.Services.AddHttpClient(HttpClientNames.MessengerServer)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
}

builder.Services.AddHostedService<EchoBotService>();

var host = builder.Build();
try
{
    await host.RunAsync();
}
finally
{
    LogManager.Shutdown();
}

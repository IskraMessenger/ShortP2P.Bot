using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.DependencyInjection;
using ShortP2P.Bot.Http;
using ShortP2P.Bot.Sample.Configuration;
using ShortP2P.Bot.Sample.Meteo;
using ShortP2P.Bot.Sample.Meteo.Commands;
using ShortP2P.Bot.Sample.Meteo.Media;
using ShortP2P.Bot.Sample.Meteo.Storage;
using ShortP2P.Bot.Sample.Meteo.Weather;
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
builder.Services.AddSingleton<IBotCertificateStore, SqliteBotCertificateStore>();
builder.Services.AddSingleton<SqliteMessageKeyStore>();
builder.Services.AddSingleton<SqliteChatStore>();
builder.Services.AddSingleton<SqliteWeatherCache>();
builder.Services.Configure<WeatherOptions>(builder.Configuration.GetSection(WeatherOptions.SectionName));
builder.Services.Configure<VoiceOptions>(builder.Configuration.GetSection(VoiceOptions.SectionName));
var voice = new VoiceOptions();
builder.Configuration.GetSection(VoiceOptions.SectionName).Bind(voice);
voice.Validate();
var weatherProvider = WeatherProviders.Parse(builder.Configuration["Weather:Provider"]);
if (weatherProvider == WeatherProviderKind.BrightSky)
    WeatherOptions.BrightSkyAddress(builder.Configuration["Weather:BrightSkyBaseUrl"]);
if (weatherProvider != WeatherProviderKind.OpenMeteo)
    WeatherOptions.OpenStreetMapAddress(builder.Configuration["Weather:OpenStreetMapBaseUrl"]);
var weatherProxyUrl = builder.Configuration["Weather:ProxyUrl"];
_ = WeatherOptions.ProxyAddress(weatherProxyUrl);

builder.Services.AddHttpClient<OpenMeteoClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.TryAddWithoutValidation(
        "User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
}).ConfigurePrimaryHttpMessageHandler(() => WeatherOptions.CreateHandler(weatherProxyUrl));
builder.Services.AddHttpClient<BrightSkyClient>(client =>
{
    client.BaseAddress = WeatherOptions.BrightSkyAddress(builder.Configuration["Weather:BrightSkyBaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ShortP2P.Bot.Sample.Meteo");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => WeatherOptions.CreateHandler(weatherProxyUrl));
builder.Services.AddHttpClient<OpenStreetMapGeocoder>(client =>
{
    client.BaseAddress = WeatherOptions.OpenStreetMapAddress(builder.Configuration["Weather:OpenStreetMapBaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ShortP2P.Bot.Sample.Meteo");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
}).ConfigurePrimaryHttpMessageHandler(() => WeatherOptions.CreateHandler(weatherProxyUrl));
builder.Services.AddSingleton<ICurrentWeatherSource>(sp =>
    weatherProvider == WeatherProviderKind.BrightSky
        ? sp.GetRequiredService<BrightSkyClient>()
        : sp.GetRequiredService<OpenMeteoClient>());
builder.Services.AddSingleton<IPlaceGeocoder>(sp =>
    weatherProvider == WeatherProviderKind.OpenMeteo
        ? sp.GetRequiredService<OpenMeteoClient>()
        : sp.GetRequiredService<OpenStreetMapGeocoder>());
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<MeteoCommandHandler>();
builder.Services.AddSingleton<WeatherAttachmentSender>();
builder.Services.AddShortP2PBot();
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

builder.Services.AddHostedService<MeteoBotService>();

var host = builder.Build();
try
{
    await host.RunAsync();
}
finally
{
    LogManager.Shutdown();
}

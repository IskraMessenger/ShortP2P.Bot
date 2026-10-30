using System.Net;
using System.Text;
using ShortP2P.Bot.Sample.Meteo.Weather;

namespace ShortP2P.Bot.Sample.Meteo.Tests;

public sealed class BrightSkyTests
{
    [Theory]
    [InlineData(null, "OpenMeteo")]
    [InlineData("", "OpenMeteo")]
    [InlineData("openmeteo", "OpenMeteo")]
    [InlineData("BrightSky", "BrightSky")]
    [InlineData(" brightsky ", "BrightSky")]
    public void Provider_ParsesKnownNames(string? configured, string expected)
    {
        Assert.Equal(expected, WeatherProviders.Name(WeatherProviders.Parse(configured)));
    }

    [Fact]
    public void Provider_RejectsUnknownName()
    {
        var error = Assert.Throws<InvalidOperationException>(() => WeatherProviders.Parse("metno"));
        Assert.Contains("Weather:Provider", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProxyUrl_EmptyMeansOff(string? configured)
    {
        Assert.Null(WeatherOptions.ProxyAddress(configured));
    }

    [Fact]
    public void ProxyUrl_AcceptsAbsoluteHttp()
    {
        var uri = WeatherOptions.ProxyAddress("http://127.0.0.1:10809");
        Assert.Equal(new Uri("http://127.0.0.1:10809"), uri);
    }

    [Fact]
    public void ProxyUrl_RejectsNonAbsoluteOrNonHttp()
    {
        var relative = Assert.Throws<InvalidOperationException>(() => WeatherOptions.ProxyAddress("127.0.0.1:10809"));
        Assert.Contains("Weather:ProxyUrl", relative.Message);

        var ftp = Assert.Throws<InvalidOperationException>(() => WeatherOptions.ProxyAddress("ftp://127.0.0.1:10809"));
        Assert.Contains("Weather:ProxyUrl", ftp.Message);
    }

    [Fact]
    public void CreateHandler_DisablesProxyWhenEmpty()
    {
        using var handler = Assert.IsType<HttpClientHandler>(WeatherOptions.CreateHandler(null));
        Assert.False(handler.UseProxy);
    }

    [Fact]
    public void CreateHandler_UsesConfiguredProxy()
    {
        using var handler = Assert.IsType<HttpClientHandler>(WeatherOptions.CreateHandler("http://127.0.0.1:10809"));
        Assert.True(handler.UseProxy);
        Assert.Equal(new Uri("http://127.0.0.1:10809"), handler.Proxy?.GetProxy(new Uri("https://api.open-meteo.com/")));
    }

    [Theory]
    [InlineData("dry", "clear-day", null, 0)]
    [InlineData("dry", "partly-cloudy-night", null, 2)]
    [InlineData("dry", "cloudy", 100d, 3)]
    [InlineData("dry", "wind", 10d, 0)]
    [InlineData("dry", null, 90d, 3)]
    [InlineData("fog", "cloudy", 10d, 45)]
    [InlineData("rain", "cloudy", null, 61)]
    [InlineData("sleet", null, null, 56)]
    [InlineData("snow", "snow", null, 71)]
    [InlineData("hail", null, null, 96)]
    [InlineData("thunderstorm", null, null, 95)]
    [InlineData(null, null, null, 0)]
    public void Codes_FollowConditionThenIconThenCloudCover(string? condition, string? icon, double? cloudCover, int code)
    {
        Assert.Equal(code, BrightSkyCodes.ToWeatherCode(condition, icon, cloudCover));
    }

    [Fact]
    public async Task Current_ReadsStationObservation()
    {
        const string json = """
            {
              "weather": {
                "timestamp": "2026-10-29T13:30:00+00:00",
                "temperature": 11.6,
                "relative_humidity": 86,
                "condition": "dry",
                "icon": "cloudy",
                "cloud_cover": 100
              }
            }
            """;
        var handler = new StubHandler(HttpStatusCode.OK, json);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.brightsky.dev/") };
        var hit = await new BrightSkyClient(http).CurrentAsync(52.52, 13.405, CancellationToken.None);

        Assert.Equal("2026-10-29 13:30", hit.ObservedAt);
        Assert.Equal(11.6, hit.TemperatureC);
        Assert.Equal(86, hit.HumidityPercent);
        Assert.Equal(3, hit.WeatherCode);
        Assert.Contains("lat=52.52", handler.RequestUri?.Query, StringComparison.Ordinal);
        Assert.Contains("lon=13.405", handler.RequestUri?.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Current_ReportsMissingStation()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, """{"detail":"no sources"}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.brightsky.dev/") };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new BrightSkyClient(http).CurrentAsync(55.75, 37.62, CancellationToken.None));
        Assert.Contains("no DWD station", error.Message);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }
}

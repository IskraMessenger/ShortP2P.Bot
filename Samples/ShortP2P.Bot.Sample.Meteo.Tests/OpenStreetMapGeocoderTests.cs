using System.Net;
using System.Text;
using ShortP2P.Bot.Sample.Meteo.Weather;

namespace ShortP2P.Bot.Sample.Meteo.Tests;

public sealed class OpenStreetMapGeocoderTests
{
    [Fact]
    public async Task Search_UsesCityAndCountry()
    {
        const string json = """
            [{
              "lat": "52.5170365",
              "lon": "13.3888599",
              "name": "Berlin",
              "display_name": "Berlin, Deutschland",
              "address": { "city": "Berlin", "country": "Deutschland" }
            }]
            """;
        var handler = new StubHandler(HttpStatusCode.OK, json);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        var hit = await new OpenStreetMapGeocoder(http).SearchAsync("Berlin", "cn", CancellationToken.None);

        Assert.NotNull(hit);
        Assert.Equal("Berlin, Deutschland", hit.Value.DisplayName);
        Assert.Equal(52.5170365, hit.Value.Latitude, 5);
        Assert.Equal(13.3888599, hit.Value.Longitude, 5);
        Assert.Contains("accept-language=zh", handler.RequestUri?.Query, StringComparison.Ordinal);
        Assert.Contains("q=Berlin", handler.RequestUri?.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_ReturnsNullWhenNothingMatches()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "[]");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        var hit = await new OpenStreetMapGeocoder(http).SearchAsync("nowhere", "ru", CancellationToken.None);
        Assert.Null(hit);
    }

    [Fact]
    public async Task Reverse_UsesTownAndIgnoresGeocodeError()
    {
        const string town = """
            {
              "lat": "52.4000000",
              "lon": "13.0000000",
              "name": "Hauptstraße",
              "address": { "town": "Potsdam", "country": "Deutschland" }
            }
            """;
        var handler = new StubHandler(HttpStatusCode.OK, town);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        var hit = await new OpenStreetMapGeocoder(http).ReverseAsync(52.4, 13, "de", CancellationToken.None);
        Assert.Equal("Potsdam, Deutschland", hit?.DisplayName);

        handler.Next(HttpStatusCode.OK, """{"error":"Unable to geocode"}""");
        var missing = await new OpenStreetMapGeocoder(http).ReverseAsync(0, 0, "de", CancellationToken.None);
        Assert.Null(missing);
    }

    [Fact]
    public void BaseUrl_RequiresHttps()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            WeatherOptions.OpenStreetMapAddress("http://nominatim.openstreetmap.org/"));
        Assert.Contains("OpenStreetMapBaseUrl", error.Message);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private HttpStatusCode _status;
        private string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public Uri? RequestUri { get; private set; }

        public void Next(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

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

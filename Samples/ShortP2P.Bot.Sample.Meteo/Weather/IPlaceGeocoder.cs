namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal interface IPlaceGeocoder
{
    Task<GeoHit?> SearchAsync(string name, string language, CancellationToken cancellationToken);

    Task<GeoHit?> ReverseAsync(double latitude, double longitude, string language, CancellationToken cancellationToken);
}

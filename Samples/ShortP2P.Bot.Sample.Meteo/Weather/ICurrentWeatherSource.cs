namespace ShortP2P.Bot.Sample.Meteo.Weather;

internal interface ICurrentWeatherSource
{
    Task<ForecastHit> CurrentAsync(double latitude, double longitude, CancellationToken cancellationToken);
}

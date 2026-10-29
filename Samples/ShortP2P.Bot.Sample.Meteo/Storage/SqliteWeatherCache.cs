using Microsoft.Data.Sqlite;
using ShortP2P.Bot.Sample.Meteo.Weather;
using ShortP2P.Bot.Sample.Storage;

namespace ShortP2P.Bot.Sample.Meteo.Storage;

internal sealed class SqliteWeatherCache
{
    public const double RadiusKm = 10;
    public static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    private readonly SampleBotDatabase _database;

    public SqliteWeatherCache(SampleBotDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS weather_cache (
                    id                INTEGER PRIMARY KEY AUTOINCREMENT,
                    kind              TEXT NOT NULL,
                    place_key         TEXT NULL,
                    latitude          REAL NOT NULL,
                    longitude         REAL NOT NULL,
                    display_name      TEXT NOT NULL,
                    temperature_c     REAL NOT NULL,
                    humidity_percent  INTEGER NOT NULL,
                    weather_code      INTEGER NOT NULL,
                    observed_at       TEXT NOT NULL,
                    fetched_at_utc    TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_weather_cache_place
                    ON weather_cache (kind, place_key);
                """;
            command.ExecuteNonQuery();
        });
    }

    public WeatherReport? TryGetPlace(string provider, string placeKey, DateTime utcNow)
    {
        var cutoff = Cutoff(utcNow);
        return _database.Execute(connection =>
        {
            Purge(connection, cutoff);
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT latitude, longitude, display_name, temperature_c, humidity_percent,
                       weather_code, observed_at, fetched_at_utc
                FROM weather_cache
                WHERE kind = $kind AND place_key = $key AND fetched_at_utc >= $cutoff
                ORDER BY fetched_at_utc DESC
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("$kind", PlaceKind(provider));
            command.Parameters.AddWithValue("$key", placeKey);
            command.Parameters.AddWithValue("$cutoff", cutoff);
            using var reader = command.ExecuteReader();
            return reader.Read() ? Read(reader) : null;
        });
    }

    public WeatherReport? TryGetCoordinates(string provider, double latitude, double longitude, DateTime utcNow)
    {
        var cutoff = Cutoff(utcNow);
        var target = new GeoPoint(latitude, longitude);
        return _database.Execute(connection =>
        {
            Purge(connection, cutoff);
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT latitude, longitude, display_name, temperature_c, humidity_percent,
                       weather_code, observed_at, fetched_at_utc
                FROM weather_cache
                WHERE kind = $kind AND fetched_at_utc >= $cutoff;
                """;
            command.Parameters.AddWithValue("$kind", CoordinatesKind(provider));
            command.Parameters.AddWithValue("$cutoff", cutoff);
            using var reader = command.ExecuteReader();
            WeatherReport? best = null;
            var bestDistance = double.MaxValue;
            while (reader.Read())
            {
                var report = Read(reader);
                var distance = GeoQuery.Kilometers(target, new GeoPoint(report.Latitude, report.Longitude));
                if (distance > RadiusKm || distance >= bestDistance)
                    continue;
                best = report;
                bestDistance = distance;
            }

            return best;
        });
    }

    public void SavePlace(string provider, string placeKey, WeatherReport report)
    {
        var kind = PlaceKind(provider);
        _database.Execute(connection =>
        {
            using var delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM weather_cache WHERE kind = $kind AND place_key = $key;";
            delete.Parameters.AddWithValue("$kind", kind);
            delete.Parameters.AddWithValue("$key", placeKey);
            delete.ExecuteNonQuery();
            Insert(connection, kind, placeKey, report);
        });
    }

    public void SaveCoordinates(string provider, WeatherReport report)
    {
        _database.Execute(connection => Insert(connection, CoordinatesKind(provider), null, report));
    }

    private static string PlaceKind(string provider) => "place:" + provider;

    private static string CoordinatesKind(string provider) => "coordinates:" + provider;

    private static void Insert(SqliteConnection connection, string kind, string? placeKey, WeatherReport report)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO weather_cache (
                kind, place_key, latitude, longitude, display_name,
                temperature_c, humidity_percent, weather_code, observed_at, fetched_at_utc)
            VALUES (
                $kind, $placeKey, $lat, $lon, $name,
                $temp, $humidity, $code, $observed, $fetched);
            """;
        command.Parameters.AddWithValue("$kind", kind);
        command.Parameters.AddWithValue("$placeKey", (object?)placeKey ?? DBNull.Value);
        command.Parameters.AddWithValue("$lat", report.Latitude);
        command.Parameters.AddWithValue("$lon", report.Longitude);
        command.Parameters.AddWithValue("$name", report.DisplayName);
        command.Parameters.AddWithValue("$temp", report.TemperatureC);
        command.Parameters.AddWithValue("$humidity", report.HumidityPercent);
        command.Parameters.AddWithValue("$code", report.WeatherCode);
        command.Parameters.AddWithValue("$observed", report.ObservedAt);
        command.Parameters.AddWithValue("$fetched", SqliteValues.FormatUtc(report.FetchedAtUtc));
        command.ExecuteNonQuery();
    }

    private static void Purge(SqliteConnection connection, string cutoff)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM weather_cache WHERE fetched_at_utc < $cutoff;";
        command.Parameters.AddWithValue("$cutoff", cutoff);
        command.ExecuteNonQuery();
    }

    private static string Cutoff(DateTime utcNow) => SqliteValues.FormatUtc(utcNow.ToUniversalTime() - MaxAge);

    private static WeatherReport Read(SqliteDataReader reader) => new()
    {
        Latitude = reader.GetDouble(0),
        Longitude = reader.GetDouble(1),
        DisplayName = reader.GetString(2),
        TemperatureC = reader.GetDouble(3),
        HumidityPercent = Convert.ToInt32(reader.GetValue(4), System.Globalization.CultureInfo.InvariantCulture),
        WeatherCode = Convert.ToInt32(reader.GetValue(5), System.Globalization.CultureInfo.InvariantCulture),
        ObservedAt = reader.GetString(6),
        FetchedAtUtc = SqliteValues.ReadDateTime(reader, "fetched_at_utc") ?? DateTime.UnixEpoch
    };
}

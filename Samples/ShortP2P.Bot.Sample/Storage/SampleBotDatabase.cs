using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using ShortP2P.Bot.Sample.Configuration;

namespace ShortP2P.Bot.Sample.Storage;

internal sealed class SampleBotDatabase
{
    private readonly string _connectionString;
    private readonly object _gate = new();

    public SampleBotDatabase(IOptions<SampleBotOptions> options)
    {
        var connectionString = options.Value.Storage.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Storage:ConnectionString is required.");

        _connectionString = connectionString;
        EnsureSchema();
    }

    public void Execute(Action<SqliteConnection> action)
    {
        lock (_gate)
        {
            using var connection = Open();
            action(connection);
        }
    }

    public T Execute<T>(Func<SqliteConnection, T> action)
    {
        lock (_gate)
        {
            using var connection = Open();
            return action(connection);
        }
    }

    private void EnsureSchema()
    {
        lock (_gate)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS messenger_servers (
                    id                       INTEGER PRIMARY KEY AUTOINCREMENT,
                    base_url                 TEXT NOT NULL UNIQUE,
                    network_id               TEXT NOT NULL,
                    bot_key                  TEXT NOT NULL,
                    is_available             INTEGER NOT NULL DEFAULT 1,
                    last_successful_at_utc   TEXT NULL,
                    registered_at_utc        TEXT NOT NULL,
                    last_health_check_at_utc TEXT NULL,
                    last_error               TEXT NULL
                );

                CREATE TABLE IF NOT EXISTS bot_certificate (
                    id                 INTEGER PRIMARY KEY CHECK (id = 1),
                    pfx                BLOB NOT NULL,
                    fingerprint_sha256 TEXT NOT NULL,
                    created_at_utc     TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS requests (
                    id                         INTEGER PRIMARY KEY AUTOINCREMENT,
                    received_at_utc            TEXT NOT NULL,
                    client_network_id          TEXT NOT NULL,
                    correlation_id             TEXT NULL,
                    message_text               TEXT NOT NULL,
                    encrypted_message_base64   TEXT NOT NULL,
                    reply_text                 TEXT NOT NULL
                );
                """;
            command.ExecuteNonQuery();
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}

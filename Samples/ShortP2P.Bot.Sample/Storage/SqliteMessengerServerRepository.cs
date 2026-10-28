using Microsoft.Data.Sqlite;
using ShortP2P.Bot.Dal;
using ShortP2P.Bot.Dal.Entities;

namespace ShortP2P.Bot.Sample.Storage;

internal sealed class SqliteMessengerServerRepository : IMessengerServerRepository
{
    private readonly SampleBotDatabase _database;

    public SqliteMessengerServerRepository(SampleBotDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public Task<MessengerServerEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindOne("id = $id", command => command.Parameters.AddWithValue("$id", id)));
    }

    public Task<MessengerServerEntity?> GetByBaseUrlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FindOne(
            "base_url = $baseUrl COLLATE NOCASE",
            command => command.Parameters.AddWithValue("$baseUrl", baseUrl)));
    }

    public Task<IReadOnlyList<MessengerServerEntity>> GetAvailableOrderedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var available = Query("is_available <> 0 AND is_blocked = 0", _ => { });
        IReadOnlyList<MessengerServerEntity> ordered = MessengerServerOrdering.OrderForMessaging(available).ToArray();
        return Task.FromResult(ordered);
    }

    public Task<IReadOnlyList<MessengerServerEntity>> GetUnavailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Query("is_available = 0 AND is_blocked = 0 ORDER BY id", _ => { }));
    }

    public Task<IReadOnlyList<MessengerServerEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Query("1 = 1 ORDER BY id", _ => { }));
    }

    public Task UpsertAsync(MessengerServerEntity server, CancellationToken cancellationToken = default)
    {
        if (server is null) throw new ArgumentNullException(nameof(server));
        cancellationToken.ThrowIfCancellationRequested();

        _database.Execute(connection =>
        {
            var existingId = FindIdByBaseUrl(connection, server.BaseUrl);
            if (existingId is null)
            {
                using var insert = connection.CreateCommand();
                insert.CommandText =
                    """
                    INSERT INTO messenger_servers (
                        base_url,
                        network_id,
                        bot_name,
                        bot_key,
                        server_fingerprint_sha256,
                        is_blocked,
                        is_available,
                        last_successful_at_utc,
                        registered_at_utc,
                        last_health_check_at_utc,
                        last_error)
                    VALUES (
                        $baseUrl,
                        $networkId,
                        $botName,
                        $botKey,
                        $serverFingerprint,
                        $isBlocked,
                        $isAvailable,
                        $lastSuccessful,
                        $registered,
                        $healthCheck,
                        $lastError);
                    """;
                BindServer(insert, server);
                insert.ExecuteNonQuery();

                using var idCommand = connection.CreateCommand();
                idCommand.CommandText = "SELECT last_insert_rowid();";
                server.Id = (long)idCommand.ExecuteScalar()!;
                return;
            }

            server.Id = existingId.Value;
            using var update = connection.CreateCommand();
            update.CommandText =
                """
                UPDATE messenger_servers
                SET base_url = $baseUrl,
                    network_id = $networkId,
                    bot_name = $botName,
                    bot_key = $botKey,
                    server_fingerprint_sha256 = $serverFingerprint,
                    is_blocked = $isBlocked,
                    is_available = $isAvailable,
                    last_successful_at_utc = $lastSuccessful,
                    registered_at_utc = $registered,
                    last_health_check_at_utc = $healthCheck,
                    last_error = $lastError
                WHERE id = $id;
                """;
            BindServer(update, server);
            update.Parameters.AddWithValue("$id", server.Id);
            update.ExecuteNonQuery();
        });

        return Task.CompletedTask;
    }

    public Task MarkUnavailableAsync(long id, string? error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExecuteUpdate(
            """
            UPDATE messenger_servers
            SET is_available = 0,
                last_error = $error
            WHERE id = $id AND is_blocked = 0;
            """,
            command =>
            {
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$error", TrimError(error));
            });
        return Task.CompletedTask;
    }

    public Task MarkBlockedAsync(long id, string? error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExecuteUpdate(
            """
            UPDATE messenger_servers
            SET is_blocked = 1,
                is_available = 0,
                last_error = $error
            WHERE id = $id;
            """,
            command =>
            {
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$error", TrimError(error));
            });
        return Task.CompletedTask;
    }

    public Task MarkAvailableAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExecuteUpdate(
            """
            UPDATE messenger_servers
            SET is_available = 1,
                last_error = NULL,
                last_health_check_at_utc = $checkedAt
            WHERE id = $id AND is_blocked = 0;
            """,
            command =>
            {
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$checkedAt", SqliteValues.FormatUtc(DateTime.UtcNow));
            });
        return Task.CompletedTask;
    }

    public Task MarkSuccessfulAsync(long id, DateTime successfulAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExecuteUpdate(
            """
            UPDATE messenger_servers
            SET is_available = 1,
                last_error = NULL,
                last_successful_at_utc = $successfulAt
            WHERE id = $id AND is_blocked = 0;
            """,
            command =>
            {
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$successfulAt", SqliteValues.FormatUtc(successfulAtUtc));
            });
        return Task.CompletedTask;
    }

    public Task UpdateHealthCheckAsync(
        long id,
        bool isAvailable,
        DateTime checkedAtUtc,
        string? error,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExecuteUpdate(
            """
            UPDATE messenger_servers
            SET is_available = $isAvailable,
                last_health_check_at_utc = $checkedAt,
                last_error = $error
            WHERE id = $id AND is_blocked = 0;
            """,
            command =>
            {
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$isAvailable", isAvailable ? 1 : 0);
                command.Parameters.AddWithValue("$checkedAt", SqliteValues.FormatUtc(checkedAtUtc));
                command.Parameters.AddWithValue("$error", TrimError(error));
            });
        return Task.CompletedTask;
    }

    private MessengerServerEntity? FindOne(string where, Action<SqliteCommand> bind)
    {
        return _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM messenger_servers WHERE " + where + " LIMIT 1;";
            bind(command);
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadServer(reader) : null;
        });
    }

    private IReadOnlyList<MessengerServerEntity> Query(string where, Action<SqliteCommand> bind)
    {
        return _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM messenger_servers WHERE " + where + ";";
            bind(command);
            using var reader = command.ExecuteReader();
            var list = new List<MessengerServerEntity>();
            while (reader.Read())
                list.Add(ReadServer(reader));
            return (IReadOnlyList<MessengerServerEntity>)list;
        });
    }

    private void ExecuteUpdate(string sql, Action<SqliteCommand> bind)
    {
        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            bind(command);
            command.ExecuteNonQuery();
        });
    }

    private static long? FindIdByBaseUrl(SqliteConnection connection, string baseUrl)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM messenger_servers WHERE base_url = $baseUrl COLLATE NOCASE LIMIT 1;";
        command.Parameters.AddWithValue("$baseUrl", baseUrl);
        var value = command.ExecuteScalar();
        return value is null or DBNull ? null : (long)value;
    }

    private static void BindServer(SqliteCommand command, MessengerServerEntity server)
    {
        command.Parameters.AddWithValue("$baseUrl", server.BaseUrl);
        command.Parameters.AddWithValue("$networkId", server.NetworkId);
        command.Parameters.AddWithValue("$botName", server.BotName);
        command.Parameters.AddWithValue("$botKey", server.BotKey);
        command.Parameters.AddWithValue("$serverFingerprint", server.ServerFingerprintSha256);
        command.Parameters.AddWithValue("$isBlocked", server.IsBlocked ? 1 : 0);
        command.Parameters.AddWithValue("$isAvailable", server.IsAvailable ? 1 : 0);
        command.Parameters.AddWithValue("$lastSuccessful", NullableUtc(server.LastSuccessfulAtUtc));
        command.Parameters.AddWithValue("$registered", SqliteValues.FormatUtc(server.RegisteredAtUtc));
        command.Parameters.AddWithValue("$healthCheck", NullableUtc(server.LastHealthCheckAtUtc));
        command.Parameters.AddWithValue("$lastError", TrimError(server.LastError));
    }

    private static object NullableUtc(DateTime? value) =>
        value is null ? DBNull.Value : SqliteValues.FormatUtc(value.Value);

    private static object TrimError(string? error)
    {
        if (string.IsNullOrEmpty(error))
            return DBNull.Value;

        return error.Length <= 1024 ? error : error[..1024];
    }

    private static MessengerServerEntity ReadServer(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("id")),
        BaseUrl = reader.GetString(reader.GetOrdinal("base_url")),
        NetworkId = reader.GetString(reader.GetOrdinal("network_id")),
        BotName = SqliteValues.ReadString(reader, "bot_name") ?? string.Empty,
        BotKey = reader.GetString(reader.GetOrdinal("bot_key")),
        ServerFingerprintSha256 = SqliteValues.ReadString(reader, "server_fingerprint_sha256") ?? string.Empty,
        IsBlocked = reader.GetInt64(reader.GetOrdinal("is_blocked")) != 0,
        IsAvailable = reader.GetInt64(reader.GetOrdinal("is_available")) != 0,
        LastSuccessfulAtUtc = SqliteValues.ReadDateTime(reader, "last_successful_at_utc"),
        RegisteredAtUtc = SqliteValues.ReadDateTime(reader, "registered_at_utc") ?? DateTime.UnixEpoch,
        LastHealthCheckAtUtc = SqliteValues.ReadDateTime(reader, "last_health_check_at_utc"),
        LastError = SqliteValues.ReadString(reader, "last_error")
    };
}

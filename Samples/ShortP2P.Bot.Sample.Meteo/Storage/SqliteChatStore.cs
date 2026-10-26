using Microsoft.Data.Sqlite;
using ShortP2P.Bot.Sample.Meteo.Commands;
using ShortP2P.Bot.Sample.Storage;

namespace ShortP2P.Bot.Sample.Meteo.Storage;

internal sealed class SqliteChatStore
{
    private readonly SampleBotDatabase _database;

    public SqliteChatStore(SampleBotDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS chats (
                    client_network_id TEXT PRIMARY KEY,
                    language          TEXT NOT NULL,
                    is_active         INTEGER NOT NULL,
                    updated_at_utc    TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS chat_messages (
                    id                INTEGER PRIMARY KEY AUTOINCREMENT,
                    client_network_id TEXT NOT NULL,
                    direction         TEXT NOT NULL,
                    body              TEXT NOT NULL,
                    created_at_utc    TEXT NOT NULL
                );
                """;
            command.ExecuteNonQuery();
        });
    }

    public ChatSession GetOrCreate(string clientNetworkId)
    {
        return _database.Execute(connection =>
        {
            using var select = connection.CreateCommand();
            select.CommandText = "SELECT language, is_active FROM chats WHERE client_network_id = $id;";
            select.Parameters.AddWithValue("$id", clientNetworkId);
            using var reader = select.ExecuteReader();
            if (reader.Read())
            {
                return new ChatSession
                {
                    ClientNetworkId = clientNetworkId,
                    Language = reader.GetString(0),
                    IsActive = reader.GetInt64(1) != 0
                };
            }

            var created = new ChatSession
            {
                ClientNetworkId = clientNetworkId,
                Language = MeteoTexts.DefaultLanguage,
                IsActive = false
            };
            Insert(connection, created);
            return created;
        });
    }

    public void Save(ChatSession session)
    {
        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE chats
                SET language = $language,
                    is_active = $active,
                    updated_at_utc = $updated
                WHERE client_network_id = $id;
                """;
            command.Parameters.AddWithValue("$language", session.Language);
            command.Parameters.AddWithValue("$active", session.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("$updated", SqliteValues.FormatUtc(DateTime.UtcNow));
            command.Parameters.AddWithValue("$id", session.ClientNetworkId);
            if (command.ExecuteNonQuery() == 0)
                Insert(connection, session);
        });
    }

    public void AddMessage(string clientNetworkId, string direction, string body)
    {
        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO chat_messages (client_network_id, direction, body, created_at_utc)
                VALUES ($id, $direction, $body, $created);
                """;
            command.Parameters.AddWithValue("$id", clientNetworkId);
            command.Parameters.AddWithValue("$direction", direction);
            command.Parameters.AddWithValue("$body", body);
            command.Parameters.AddWithValue("$created", SqliteValues.FormatUtc(DateTime.UtcNow));
            command.ExecuteNonQuery();
        });
    }

    private static void Insert(SqliteConnection connection, ChatSession session)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO chats (client_network_id, language, is_active, updated_at_utc)
            VALUES ($id, $language, $active, $updated);
            """;
        command.Parameters.AddWithValue("$id", session.ClientNetworkId);
        command.Parameters.AddWithValue("$language", session.Language);
        command.Parameters.AddWithValue("$active", session.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("$updated", SqliteValues.FormatUtc(DateTime.UtcNow));
        command.ExecuteNonQuery();
    }
}

using Microsoft.Data.Sqlite;

namespace ShortP2P.Bot.Sample.Storage;

internal sealed class SqliteMessageKeyStore
{
    private readonly SampleBotDatabase _database;

    public SqliteMessageKeyStore(SampleBotDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                CREATE TABLE IF NOT EXISTS message_keys (
                    id           INTEGER PRIMARY KEY CHECK (id = 1),
                    public_json  TEXT NOT NULL,
                    private_json TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL
                );
                """;
            cmd.ExecuteNonQuery();
        });
    }

    public (string PublicJson, string PrivateJson)? TryGet()
    {
        return _database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT public_json, private_json FROM message_keys WHERE id = 1;";
            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return ((string, string)?)null;
            return (reader.GetString(0), reader.GetString(1));
        });
    }

    public void Save(string publicJson, string privateJson)
    {
        _database.Execute(conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                INSERT INTO message_keys (id, public_json, private_json, created_at_utc)
                VALUES (1, $pub, $priv, $created)
                ON CONFLICT(id) DO UPDATE SET
                    public_json = excluded.public_json,
                    private_json = excluded.private_json,
                    created_at_utc = excluded.created_at_utc;
                """;
            cmd.Parameters.AddWithValue("$pub", publicJson);
            cmd.Parameters.AddWithValue("$priv", privateJson);
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        });
    }
}

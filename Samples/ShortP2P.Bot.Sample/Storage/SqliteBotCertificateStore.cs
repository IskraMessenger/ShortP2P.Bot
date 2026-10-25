using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Data.Sqlite;

namespace ShortP2P.Bot.Sample.Storage;

internal sealed class SqliteBotCertificateStore : IBotCertificateStore
{
    private readonly SampleBotDatabase _database;
    private readonly ILogger<SqliteBotCertificateStore> _logger;

    public SqliteBotCertificateStore(SampleBotDatabase database, ILogger<SqliteBotCertificateStore> logger)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public BotCertificateRecord GetOrCreate(string subjectCommonName)
    {
        var existing = _database.Execute(Load);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Loaded bot certificate from the local database, fingerprint {Fingerprint}",
                existing.FingerprintSha256);
            return existing;
        }

        var created = BotCertificateFactory.Create(subjectCommonName);
        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO bot_certificate (id, pfx, fingerprint_sha256, created_at_utc)
                VALUES (1, $pfx, $fingerprint, $createdAt);
                """;
            command.Parameters.Add("$pfx", SqliteType.Blob).Value = created.Pfx;
            command.Parameters.AddWithValue("$fingerprint", created.FingerprintSha256);
            command.Parameters.AddWithValue("$createdAt", SqliteValues.FormatUtc(DateTime.UtcNow));
            command.ExecuteNonQuery();
        });

        _logger.LogInformation(
            "Created bot certificate and saved it in the local database, fingerprint {Fingerprint}",
            created.FingerprintSha256);
        return created;
    }

    private static BotCertificateRecord? Load(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT pfx, fingerprint_sha256 FROM bot_certificate WHERE id = 1;";
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var pfx = (byte[])reader["pfx"];
        var storedFingerprint = reader.GetString(reader.GetOrdinal("fingerprint_sha256"));
        using var certificate = new X509Certificate2(pfx);
        var fingerprint = Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant();

        if (!string.Equals(fingerprint, storedFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Stored bot certificate fingerprint does not match the certificate.");

        return new BotCertificateRecord
        {
            Pfx = pfx,
            FingerprintSha256 = fingerprint
        };
    }
}

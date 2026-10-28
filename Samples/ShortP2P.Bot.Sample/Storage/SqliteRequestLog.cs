namespace ShortP2P.Bot.Sample.Storage;

internal sealed class SqliteRequestLog : IRequestLog
{
    private readonly SampleBotDatabase _database;

    public SqliteRequestLog(SampleBotDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public Task AddAsync(IncomingRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        cancellationToken.ThrowIfCancellationRequested();

        _database.Execute(connection =>
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO requests (
                    received_at_utc,
                    client_network_id,
                    correlation_id,
                    message_text,
                    encrypted_message_base64,
                    reply_text)
                VALUES (
                    $receivedAt,
                    $clientNetworkId,
                    $correlationId,
                    $messageText,
                    $encryptedMessage,
                    $replyText);
                """;
            command.Parameters.AddWithValue("$receivedAt", SqliteValues.FormatUtc(request.ReceivedAtUtc));
            command.Parameters.AddWithValue("$clientNetworkId", request.ClientNetworkId);
            command.Parameters.AddWithValue("$correlationId", (object?)request.CorrelationId ?? DBNull.Value);
            command.Parameters.AddWithValue("$messageText", request.MessageText);
            command.Parameters.AddWithValue("$encryptedMessage", request.EncryptedMessageBase64);
            command.Parameters.AddWithValue("$replyText", request.ReplyText);
            command.ExecuteNonQuery();
        });

        return Task.CompletedTask;
    }
}

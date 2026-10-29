namespace ShortP2P.Bot.Contracts;

/// <summary>HTTPS routes of ShortP2P.MessengerServer.Api (v1), mirrored for netstandard2.1.</summary>
public static class ServerApiRoutes
{
    public const string Prefix = "/api/v1";

    public const string HealthPing = Prefix + "/health/ping";

    public const string ServerCertificate = Prefix + "/server/certificate";

    public const string Blobs = Prefix + "/blobs";

    public const string BotRegister = Prefix + "/bot_server_interaction/register";
    public const string BotLogin = Prefix + "/bot_server_interaction/login";
    public const string BotRemove = Prefix + "/bot_server_interaction/remove";

    public const string BotWaitForIncomeMessages = Prefix + "/bot_server_interaction/wait_for_income_messages";
    public const string BotSendMessages = Prefix + "/bot_server_interaction/send_messages";

    /// <summary>POST bot receipt for a client → bot message (auth via <c>botKey</c>); drops the message and its chunks.</summary>
    public const string BotReceipts = Prefix + "/bot_server_interaction/receipts";
}

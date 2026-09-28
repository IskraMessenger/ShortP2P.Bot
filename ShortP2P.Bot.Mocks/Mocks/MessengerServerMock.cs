using System.Net;
using System.Text.Json;
using ShortP2P.Bot.Contracts;
using ShortP2P.Bot.Contracts.Dtos;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace ShortP2P.Bot.Mocks.Mocks;

/// <summary>
/// WireMock stand-in for ShortP2P.MessengerServer.Api (bot endpoints used by this library).
/// </summary>
public sealed class MessengerServerMock : IDisposable
{
    public const string DefaultBotKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    public const string DefaultToken = "mock-bot-token";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly WireMockServer _server;

    private MessengerServerMock(WireMockServer server)
    {
        _server = server;
    }

    public WireMockServer Server => _server;

    public Uri BaseUri => new(_server.Urls[0].TrimEnd('/'), UriKind.Absolute);

    public static MessengerServerMock Start() => new(WireMockServer.Start());

    /// <summary>Stubs ping, register, login, send and wait with successful defaults.</summary>
    public MessengerServerMock SetupHappyPath(IReadOnlyList<BotClientMessageDto>? incomeMessages = null)
    {
        SetupPingOk();
        SetupRegister();
        SetupLogin();
        SetupSendMessages();
        SetupWaitForIncomeMessages(incomeMessages);
        return this;
    }

    public MessengerServerMock SetupPingOk()
    {
        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.HealthPing).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));
        return this;
    }

    public MessengerServerMock SetupPingUnavailable()
    {
        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.HealthPing).UsingGet())
            .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.ServiceUnavailable));
        return this;
    }

    public MessengerServerMock SetupRegister(string? botKey = null)
    {
        var key = botKey ?? DefaultBotKey;

        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.BotRegister).UsingPost())
            .RespondWith(Response.Create().WithCallback(request =>
            {
                var payload = DeserializeOrDefault<BotRegisterRequest>(request.Body);
                return JsonResponse(201, new BotRegisterResponse
                {
                    NetworkId = payload.NetworkId,
                    BotName = payload.BotName,
                    BotReadableName = payload.BotReadableName,
                    BotDescription = payload.BotDescription,
                    BotKey = key
                });
            }));

        return this;
    }

    public MessengerServerMock SetupLogin(string? token = null, DateTime? expiresAtUtc = null)
    {
        var issuedToken = token ?? DefaultToken;
        var expires = expiresAtUtc ?? DateTime.UtcNow.AddHours(1);

        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.BotLogin).UsingPost())
            .RespondWith(Json(200, new BotLoginResponse
            {
                Token = issuedToken,
                ExpiresAtUtc = expires
            }));

        return this;
    }

    public MessengerServerMock SetupSendMessages()
    {
        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.BotSendMessages).UsingPost())
            .RespondWith(Response.Create().WithCallback(request =>
            {
                var payload = DeserializeOrDefault<BotSendMessagesRequest>(request.Body);
                return JsonResponse(200, new BotSendMessagesResponse { RequestId = payload.RequestId });
            }));

        return this;
    }

    public MessengerServerMock SetupWaitForIncomeMessages(IReadOnlyList<BotClientMessageDto>? messages = null)
    {
        var inbox = messages ?? Array.Empty<BotClientMessageDto>();

        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.BotWaitForIncomeMessages).UsingPost())
            .RespondWith(Response.Create().WithCallback(request =>
            {
                var payload = DeserializeOrDefault<BotWaitForIncomeMessagesRequest>(request.Body);
                return JsonResponse(200, new BotWaitForIncomeMessagesResponse
                {
                    RequestId = payload.RequestId,
                    Messages = inbox
                });
            }));

        return this;
    }

    public MessengerServerMock SetupSendMessagesUnavailable()
    {
        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.BotSendMessages).UsingPost())
            .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.ServiceUnavailable));
        return this;
    }

    public MessengerServerMock SetupWaitForIncomeMessagesUnavailable()
    {
        _server
            .Given(Request.Create().WithPath(ServerApiRoutes.BotWaitForIncomeMessages).UsingPost())
            .RespondWith(Response.Create().WithStatusCode((int)HttpStatusCode.ServiceUnavailable));
        return this;
    }

    public void ResetMappings() => _server.Reset();

    public void Dispose() => _server.Dispose();

    private static IResponseBuilder Json(int statusCode, object body) =>
        Response.Create()
            .WithStatusCode(statusCode)
            .WithHeader("Content-Type", "application/json")
            .WithBody(JsonSerializer.Serialize(body, JsonOptions));

    private static ResponseMessage JsonResponse(int statusCode, object body) => new()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, WireMockList<string>>
        {
            ["Content-Type"] = new("application/json")
        },
        BodyData = new BodyData
        {
            BodyAsString = JsonSerializer.Serialize(body, JsonOptions),
            DetectedBodyType = BodyType.String
        }
    };

    private static T DeserializeOrDefault<T>(string? json) where T : new()
    {
        if (string.IsNullOrWhiteSpace(json))
            return new T();

        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
    }
}

using Microsoft.Extensions.Options;
using ShortP2P.Bot;
using ShortP2P.Bot.Commands;

namespace ShortP2P.Bot.Tests;

public sealed class BotCommandCatalogTests
{
    [Fact]
    public void GetBotCommands_ReadsFile_AndChecksArgumentTypes()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ShortP2P.Bot.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "bot_commands");
        File.WriteAllText(
            path,
            """
            [
              {
                "command": "/GetText",
                "args": [
                  { "argName": "parameter1", "argType": "string" },
                  { "argName": "count", "argType": "number" }
                ],
                "description": "Read text"
              },
              {
                "command": "/when",
                "args": [ { "argName": "at", "argType": "datetime" } ],
                "description": "A moment"
              },
              {
                "command": "/any",
                "args": [ { "argName": "value", "argType": "dynamic" } ],
                "description": "Anything"
              },
              { "command": "/start", "args": null, "description": "Start" }
            ]
            """);

        try
        {
            var catalog = new BotCommandCatalog(Options.Create(new BotHostOptions
            {
                CommandsFile = path
            }));
            var listed = new GetBotCommands(catalog).Execute(new GetBotCommandsQuery("bot-net"));

            Assert.Equal("bot-net", listed.BotNetworkId);
            var getTextArgs = listed.Commands[0].Args;
            Assert.NotNull(getTextArgs);
            Assert.Equal("parameter1", getTextArgs[0].ArgName);
            Assert.Equal("number", getTextArgs[1].ArgType);
            Assert.Null(listed.Commands[3].Args);

            Assert.True(catalog.Inspect("/GetText Parameter1 12892").IsAccepted);
            Assert.True(catalog.Inspect("/GetText hello -12.5").IsAccepted);
            AssertRejected(catalog, "/GetText Parameter1");
            AssertRejected(catalog, "/GetText Parameter1 nope");
            AssertRejected(catalog, "/GetText Parameter1 12892 extra");

            Assert.True(catalog.Inspect("/when 2026-10-30").IsAccepted);
            Assert.True(catalog.Inspect("/when 30.10.2026 18:04").IsAccepted);
            AssertRejected(catalog, "/when tomorrow");

            Assert.True(catalog.Inspect("/any 12892").IsAccepted);
            Assert.True(catalog.Inspect("/any not-a-date").IsAccepted);
            AssertRejected(catalog, "/any");

            Assert.True(catalog.Inspect("/START").IsAccepted);
            AssertRejected(catalog, "/start now");
            AssertRejected(catalog, "/missing");

            var plain = catalog.Inspect("hello");
            Assert.False(plain.IsCommand);
            Assert.False(plain.IsAccepted);
            Assert.Null(plain.RejectionCode);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenFileMissingAndRequired_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), "ShortP2P.Bot.Tests", Guid.NewGuid().ToString("N"), "bot_commands");
        var catalog = new BotCommandCatalog(Options.Create(new BotHostOptions
        {
            CommandsFile = path,
            RequireCommandsFile = true
        }));

        var ex = Assert.Throws<InvalidOperationException>(() => catalog.Commands);
        Assert.Contains("was not found", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_WhenArgumentTypeUnknown_Throws()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ShortP2P.Bot.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "bot_commands");
        File.WriteAllText(
            path,
            """[{ "command": "/x", "args": [{ "argName": "n", "argType": "integer" }], "description": "X" }]""");

        try
        {
            var catalog = new BotCommandCatalog(Options.Create(new BotHostOptions { CommandsFile = path }));
            Assert.Throws<InvalidOperationException>(() => catalog.Commands);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void AssertRejected(BotCommandCatalog catalog, string text)
    {
        var inspection = catalog.Inspect(text);
        Assert.True(inspection.IsCommand);
        Assert.False(inspection.IsAccepted);
        Assert.Equal(BotCommandRejection.Code, inspection.RejectionCode);
        Assert.StartsWith(BotCommandRejection.Code + ":", inspection.RejectionMessage);
    }
}

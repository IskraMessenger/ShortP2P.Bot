using System.Text;
using System.Text.Json;
using ShortP2P.Bot.Contracts;
using ShortP2P.Bot.Sample.Echo;
using ShortP2P.Bot.Sample.Meteo.Media;

namespace ShortP2P.Bot.Sample.Meteo.Tests;

public sealed class WeatherMediaTests
{
    [Theory]
    [InlineData("sun")]
    [InlineData("cloudy")]
    [InlineData("rain")]
    [InlineData("drizzle")]
    [InlineData("snow")]
    [InlineData("sleet")]
    [InlineData("fog")]
    [InlineData("thunderstorm")]
    [InlineData("dry")]
    public void Picture_IsPngBetween100And150Kb(string condition)
    {
        var png = WeatherPicture.Render(condition);
        Assert.InRange(png.Length, WeatherPicture.MinBytes, WeatherPicture.MaxBytes);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png.AsSpan(0, 8).ToArray());
    }

    [Fact]
    public void Picture_DiffersByCondition()
    {
        var sun = WeatherPicture.Render("sun");
        var rain = WeatherPicture.Render("rain");
        var snow = WeatherPicture.Render("snow");
        Assert.NotEqual(sun, rain);
        Assert.NotEqual(rain, snow);
    }

    [Fact]
    public void Chunks_SplitOnFiftyKilobytes()
    {
        var payload = new byte[BlobLimits.ChunkSizeBytes + 10];
        payload[0] = 1;
        payload[^1] = 2;
        var chunks = BlobChunks.Split(payload);
        Assert.Equal(2, chunks.Length);
        Assert.Equal(BlobLimits.ChunkSizeBytes, chunks[0].Length);
        Assert.Equal(10, chunks[1].Length);
        Assert.Equal(1, chunks[0][0]);
        Assert.Equal(2, chunks[1][^1]);
    }

    [Fact]
    public void TransferOffer_UsesClientPropertyNames()
    {
        var png = new byte[] { 1, 2, 3, 4 };
        var inner = BotMessageCrypto.EncodeChatWireImage("image/png", png);
        Assert.Equal((byte)'S', inner[0]);
        Assert.Equal(0x02, inner[4]);

        var offer = BotMessageCrypto.EncodeChatWireTransferOffer(new ChatWireTransferOffer(
            "abc",
            "deadbeef",
            "image",
            "weather.png",
            "image/png",
            png.Length,
            "https://127.0.0.1:51111",
            0,
            1,
            "abc",
            3,
            "deadbeef"));
        Assert.Equal(0x04, offer[4]);
        var jsonLen = BitConverter.ToInt32(offer, 5);
        var json = Encoding.UTF8.GetString(offer, 9, jsonLen);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("abc", doc.RootElement.GetProperty("TransferId").GetString());
        Assert.Equal("image", doc.RootElement.GetProperty("PayloadKind").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("TotalChunks").GetInt32());
        Assert.Equal("deadbeef", doc.RootElement.GetProperty("CiphertextSha256").GetString());
        Assert.Equal("https://127.0.0.1:51111", doc.RootElement.GetProperty("Host").GetString());
    }

    [Fact]
    public async Task Voice_IsM4aNearTarget80Kbps()
    {
        NAudio.MediaFoundation.MediaFoundationApi.Startup();
        var selected = WeatherVoice.NearestAacBitRate(
            WeatherVoice.OutputSampleRate,
            channels: 1,
            WeatherVoice.TargetBitRate);
        // Windows MF AAC at 44.1 kHz mono exposes 48/64/96… kbit/s — no exact 80; 64 wins the tie with 96.
        Assert.True(selected is 64_000 or 96_000, $"Unexpected nearest rate: {selected}");

        var script = "Hello. Temperature one degree Celsius. Humidity seventy percent. All the best!";
        var m4a = await WeatherVoice.SynthesizeAsync("en", script, CancellationToken.None);
        Assert.True(m4a.Length > 256);
        Assert.Equal("ftyp", Encoding.ASCII.GetString(m4a, 4, 4));

        var kbps = AverageBitRateKbps(m4a);
        Assert.InRange(kbps, selected / 1000 - 16, selected / 1000 + 16);
    }

    /// <summary>File size over the mdhd duration. Container overhead is small once the clip is a few seconds long.</summary>
    private static int AverageBitRateKbps(byte[] m4a)
    {
        var mdhd = IndexOf(m4a, "mdhd"u8.ToArray());
        Assert.True(mdhd > 4, "m4a has no mdhd box");
        var version = m4a[mdhd + 4];
        int timescale;
        long duration;
        if (version == 0)
        {
            timescale = (int)ReadU32(m4a, mdhd + 16);
            duration = ReadU32(m4a, mdhd + 20);
        }
        else
        {
            timescale = (int)ReadU32(m4a, mdhd + 24);
            duration = ((long)ReadU32(m4a, mdhd + 28) << 32) | ReadU32(m4a, mdhd + 32);
        }

        Assert.True(timescale > 0 && duration > 0, "m4a duration is empty");
        var seconds = duration / (double)timescale;
        return (int)(m4a.Length * 8 / seconds / 1000);
    }

    private static int IndexOf(byte[] data, byte[] needle)
    {
        for (var i = 0; i <= data.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (data[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return i;
        }

        return -1;
    }

    private static uint ReadU32(byte[] data, int offset) =>
        ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
}

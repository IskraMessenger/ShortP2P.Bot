using System.Drawing;
using System.Text;
using System.Text.Json;
using Concentus;
using Concentus.Oggfile;
using NAudio.Wave;
using ShortP2P.Bot.Contracts;
using ShortP2P.Bot.Sample.Echo;
using ShortP2P.Bot.Sample.Meteo.Commands;
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
    public void Picture_IsPlainDecodablePng(string condition)
    {
        var png = WeatherPicture.Render(condition);
        Assert.InRange(png.Length, 1024, 64 * 1024);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png.AsSpan(0, 8).ToArray());
        Assert.Equal(WeatherPicture.SceneWidth, ReadPngInt(png, 16));
        Assert.Equal(WeatherPicture.SceneHeight, ReadPngInt(png, 20));
        Assert.Equal("IEND", Encoding.ASCII.GetString(png, png.Length - 8, 4));
        Assert.True(IndexOf(png, "meTa"u8.ToArray()) < 0, "PNG still contains a private padding chunk");
#pragma warning disable CA1416
        using var stream = new MemoryStream(png);
        using var image = Image.FromStream(stream);
        Assert.Equal(WeatherPicture.SceneWidth, image.Width);
        Assert.Equal(WeatherPicture.SceneHeight, image.Height);
#pragma warning restore CA1416
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
    public void Archive_WritesPngAndVoiceUnderSaved()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 };
        var voice = new VoiceClip(
            new byte[] { 0x4F, 0x67, 0x67, 0x53, 1, 2, 3 },
            VoiceFormat.Ogg,
            VoiceOptions.DefaultBitRate);
        var paths = WeatherMediaArchive.Save("rain", "ru", png, voice);
        Assert.Equal(2, paths.Count);
        foreach (var path in paths)
        {
            Assert.StartsWith(WeatherMediaArchive.RootDirectory, path);
            Assert.True(File.Exists(path));
            Assert.True(path.EndsWith(".png") || path.EndsWith(".ogg"));
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(null, "Ogg")]
    [InlineData("", "Ogg")]
    [InlineData("ogg", "Ogg")]
    [InlineData("OGG", "Ogg")]
    [InlineData("m4a", "M4a")]
    [InlineData("MP3", "Mp3")]
    public void VoiceFormat_ParsesKnownNames(string? text, string expected) =>
        Assert.Equal(expected, VoiceOptions.Parse(text).ToString());

    [Fact]
    public void VoiceFormat_RejectsUnknownName()
    {
        var error = Assert.Throws<InvalidOperationException>(() => VoiceOptions.Parse("wav"));
        Assert.Contains("ogg", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void VoiceBitRate_RejectsOutOfRange()
    {
        Assert.Throws<InvalidOperationException>(() => VoiceOptions.RequireBitRate(1_000));
        Assert.Throws<InvalidOperationException>(() => VoiceOptions.RequireBitRate(400_000));
        Assert.Equal(VoiceOptions.DefaultBitRate, VoiceOptions.RequireBitRate(VoiceOptions.DefaultBitRate));
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
    public async Task Voice_OggIsPlayableNearConfiguredBitRate()
    {
        var script = MeteoTexts.VoiceScript("en", 1.0, 70, 3);
        var clip = await WeatherVoice.SynthesizeAsync(
            "en",
            script,
            VoiceFormat.Ogg,
            VoiceOptions.DefaultBitRate,
            CancellationToken.None);
        Assert.Equal(VoiceFormat.Ogg, clip.Format);
        Assert.Equal("weather.ogg", clip.FileName);
        Assert.Equal("audio/ogg", clip.MimeType);
        Assert.Equal(VoiceOptions.DefaultBitRate, clip.BitRate);
        Assert.True(clip.Bytes.Length > 64);
        Assert.Equal("OggS", Encoding.ASCII.GetString(clip.Bytes, 0, 4));
        Assert.True(IndexOf(clip.Bytes, "OpusHead"u8.ToArray()) >= 0, "ogg is missing OpusHead");

        var decoded = DecodeOpus(clip.Bytes);
        Assert.True(decoded.MeanAbsolute > 80, "ogg decoded to silence");
        var kbps = (int)(clip.Bytes.Length * 8 / decoded.Seconds / 1000);
        Assert.InRange(kbps, 24, 128);
    }

    [Fact]
    public async Task Voice_M4aIsAlignedNearRequestedBitRate()
    {
        NAudio.MediaFoundation.MediaFoundationApi.Startup();
        const int requested = 80_000;
        var selected = WeatherVoice.NearestAacBitRate(WeatherVoice.OutputSampleRate, channels: 1, requested);
        // Windows MF AAC at 44.1 kHz mono exposes 48/64/96… kbit/s — no exact 80; 64 wins the tie with 96.
        Assert.True(selected is 64_000 or 96_000, $"Unexpected nearest rate: {selected}");

        var script = MeteoTexts.VoiceScript("en", 1.0, 70, 3);
        var clip = await WeatherVoice.SynthesizeAsync("en", script, VoiceFormat.M4a, requested, CancellationToken.None);
        Assert.Equal(selected, clip.BitRate);
        Assert.Equal("weather.m4a", clip.FileName);
        Assert.Equal("audio/mp4", clip.MimeType);
        var m4a = clip.Bytes;
        Assert.True(m4a.Length > 256);
        Assert.Equal("ftyp", Encoding.ASCII.GetString(m4a, 4, 4));
        Assert.Equal("M4A ", Encoding.ASCII.GetString(m4a, 8, 4));
        var moov = IndexOf(m4a, "moov"u8.ToArray());
        var mdat = IndexOf(m4a, "mdat"u8.ToArray());
        Assert.True(moov >= 0 && mdat > moov, "m4a moov does not precede mdat");
        Assert.True(IndexOf(m4a, "uuid"u8.ToArray()) < 0, "m4a still contains a Media Foundation uuid box");
        AssertAlignedMp4(m4a);

        var kbps = AverageBitRateKbps(m4a);
        Assert.InRange(kbps, selected / 1000 - 16, selected / 1000 + 16);
        Assert.True(MeanAbsoluteSample(m4a, ".m4a") > 80, "m4a decoded to silence");
    }

    [Fact]
    public async Task Voice_Mp3UsesNearestMediaFoundationRate()
    {
        NAudio.MediaFoundation.MediaFoundationApi.Startup();
        var selected = WeatherVoice.NearestMp3BitRate(
            WeatherVoice.OutputSampleRate,
            channels: 1,
            VoiceOptions.DefaultBitRate);
        var script = MeteoTexts.VoiceScript("en", 1.0, 70, 3);
        var clip = await WeatherVoice.SynthesizeAsync(
            "en",
            script,
            VoiceFormat.Mp3,
            VoiceOptions.DefaultBitRate,
            CancellationToken.None);
        Assert.Equal(selected, clip.BitRate);
        Assert.Equal("weather.mp3", clip.FileName);
        Assert.Equal("audio/mpeg", clip.MimeType);
        Assert.True(clip.Bytes.Length > 256);
        Assert.True(MeanAbsoluteSample(clip.Bytes, ".mp3") > 80, "mp3 decoded to silence");
    }

    private static int ReadPngInt(byte[] png, int offset) =>
        (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];

    private static (double MeanAbsolute, double Seconds) DecodeOpus(byte[] ogg)
    {
        using var stream = new MemoryStream(ogg);
        var decoder = OpusCodecFactory.CreateDecoder(WeatherVoice.OpusSampleRate, 1);
        var reader = new OpusOggReadStream(decoder, stream);
        long sum = 0;
        long count = 0;
        while (reader.HasNextPacket)
        {
            var packet = reader.DecodeNextPacket();
            if (packet is null || packet.Length == 0)
                continue;
            foreach (var sample in packet)
            {
                sum += Math.Abs(sample);
                count++;
            }
        }

        Assert.True(count > WeatherVoice.OpusSampleRate / 2, "ogg decoded to almost no audio");
        return (sum / (double)count, count / (double)WeatherVoice.OpusSampleRate);
    }

    private static double MeanAbsoluteSample(byte[] audio, string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), "meteo-voice-" + Guid.NewGuid().ToString("N") + extension);
        File.WriteAllBytes(path, audio);
        try
        {
            using var reader = new MediaFoundationReader(path);
            var buffer = new byte[Math.Max(2, reader.WaveFormat.AverageBytesPerSecond)];
            long sum = 0;
            long count = 0;
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                var samples = read / 2;
                for (var i = 0; i < samples; i++)
                {
                    sum += Math.Abs(BitConverter.ToInt16(buffer, i * 2));
                    count++;
                }
            }

            Assert.True(count > reader.WaveFormat.SampleRate, "audio decoded to almost no samples");
            return count == 0 ? 0 : sum / (double)count;
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
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

    private static void AssertAlignedMp4(byte[] file)
    {
        var index = 0;
        while (index + 8 <= file.Length)
        {
            var size = (int)ReadU32(file, index);
            Assert.True(size >= 8 && size % 4 == 0 && index + size <= file.Length, $"box at {index} is unaligned");
            index += size;
        }

        Assert.Equal(file.Length, index);
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

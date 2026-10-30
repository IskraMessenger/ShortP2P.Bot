using System.Buffers.Binary;
using System.Globalization;
using System.Speech.Synthesis;
using System.Text;
using Concentus;
using Concentus.Enums;
using Concentus.Oggfile;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>
/// Speaks the weather script and encodes it as Ogg Opus, AAC in an m4a container, or MP3.
/// Opus keeps the requested bitrate. Media Foundation AAC and MP3 only expose discrete rates,
/// so those encoders pick the nearest supported rate after resampling to 44.1 kHz mono.
/// The AAC sink writes odd-sized uuid/hdlr boxes, so m4a bytes are rewritten 4-byte aligned.
/// </summary>
internal static class WeatherVoice
{
    public const int OutputSampleRate = 44_100;
    public const int OpusSampleRate = 48_000;

    /// <summary>Opus rejects a target above 300 kbit/s per channel.</summary>
    private const int OpusMaxBitRate = 300_000;

    private static readonly object EncoderGate = new();
    private static bool _encoderReady;

    public static Task<VoiceClip> SynthesizeAsync(
        string language,
        string script,
        VoiceFormat format,
        int bitRate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException("Voice script is empty.", nameof(script));
        VoiceOptions.RequireBitRate(bitRate);

        return Task.Run(() => Synthesize(language, script, format, bitRate), cancellationToken);
    }

    private static VoiceClip Synthesize(string language, string script, VoiceFormat format, int bitRate)
    {
        using var wav = new MemoryStream();
        using (var synth = new SpeechSynthesizer())
        {
            synth.Rate = 0;
            synth.Volume = 100;
            synth.SetOutputToWaveStream(wav);
            Speak(synth, language, script);
        }

        if (wav.Length < 64)
            throw new InvalidOperationException("Speech synthesis produced an empty wave.");

        wav.Position = 0;
        return format switch
        {
            VoiceFormat.M4a => EncodeMediaFoundation(wav, bitRate, VoiceFormat.M4a),
            VoiceFormat.Mp3 => EncodeMediaFoundation(wav, bitRate, VoiceFormat.Mp3),
            _ => EncodeOgg(wav, bitRate)
        };
    }

    private static VoiceClip EncodeOgg(MemoryStream wav, int bitRate)
    {
        EnsureEncoder();
        short[] pcm;
        using (var reader = new WaveFileReader(wav))
        {
            var target = new WaveFormat(OpusSampleRate, 16, 1);
            using var resampler = new MediaFoundationResampler(reader, target) { ResamplerQuality = 60 };
            pcm = ReadMonoPcm16(resampler);
        }

        if (pcm.Length < OpusSampleRate / 5)
            throw new InvalidOperationException("Speech synthesis produced an empty wave.");

        var opusBitRate = Math.Min(bitRate, OpusMaxBitRate);
        var encoder = OpusCodecFactory.CreateEncoder(OpusSampleRate, 1, OpusApplication.OPUS_APPLICATION_VOIP);
        encoder.Bitrate = opusBitRate;
        encoder.SignalType = OpusSignal.OPUS_SIGNAL_VOICE;
        encoder.UseVBR = true;
        encoder.UseConstrainedVBR = true;
        encoder.UseDTX = false;

        using var output = new MemoryStream();
        var ogg = new OpusOggWriteStream(encoder, output, new OpusTags(), OpusSampleRate, leaveOpen: true);
        ogg.WriteSamples(pcm, 0, pcm.Length);
        ogg.Finish();
        var encoded = output.ToArray();
        if (encoded.Length < 32 || !encoded.AsSpan(0, 4).SequenceEqual("OggS"u8))
            throw new InvalidOperationException("Opus encoder produced an empty file.");

        return new VoiceClip(encoded, VoiceFormat.Ogg, opusBitRate);
    }

    private static VoiceClip EncodeMediaFoundation(MemoryStream wav, int desiredBitRate, VoiceFormat format)
    {
        var dir = Path.Combine(Path.GetTempPath(), "meteo-voice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "weather" + (format == VoiceFormat.Mp3 ? ".mp3" : ".m4a"));
        try
        {
            EnsureEncoder();
            using var reader = new WaveFileReader(wav);
            var target = new WaveFormat(OutputSampleRate, 16, 1);
            using var resampler = new MediaFoundationResampler(reader, target) { ResamplerQuality = 60 };
            var subtype = format == VoiceFormat.Mp3
                ? AudioSubtypes.MFAudioFormat_MP3
                : AudioSubtypes.MFAudioFormat_AAC;
            var bitRate = NearestBitRate(subtype, target.SampleRate, target.Channels, desiredBitRate);
            if (format == VoiceFormat.Mp3)
                MediaFoundationEncoder.EncodeToMp3(resampler, path, bitRate);
            else
                MediaFoundationEncoder.EncodeToAac(resampler, path, bitRate);

            var encoded = File.ReadAllBytes(path);
            if (encoded.Length < 32)
                throw new InvalidOperationException($"{format} encoder produced an empty file.");
            if (format == VoiceFormat.M4a)
                encoded = ToStandardM4a(encoded);
            return new VoiceClip(encoded, format, bitRate);
        }
        finally
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static short[] ReadMonoPcm16(IWaveProvider source)
    {
        if (source.WaveFormat.BitsPerSample != 16 || source.WaveFormat.Channels != 1)
            throw new InvalidOperationException("Expected 16-bit mono PCM.");

        using var buffer = new MemoryStream();
        var chunk = new byte[Math.Max(2, source.WaveFormat.AverageBytesPerSecond)];
        int read;
        while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
            buffer.Write(chunk, 0, read);

        var bytes = buffer.ToArray();
        if (bytes.Length < 2 || bytes.Length % 2 != 0)
            throw new InvalidOperationException("Resampler produced an incomplete PCM buffer.");

        var samples = new short[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
        return samples;
    }

    /// <summary>Nearest Media Foundation AAC bitrate for the given PCM layout.</summary>
    public static int NearestAacBitRate(int sampleRate, int channels, int desiredBitRate) =>
        NearestBitRate(AudioSubtypes.MFAudioFormat_AAC, sampleRate, channels, desiredBitRate);

    /// <summary>Nearest Media Foundation MP3 bitrate for the given PCM layout.</summary>
    public static int NearestMp3BitRate(int sampleRate, int channels, int desiredBitRate) =>
        NearestBitRate(AudioSubtypes.MFAudioFormat_MP3, sampleRate, channels, desiredBitRate);

    private static int NearestBitRate(Guid subtype, int sampleRate, int channels, int desiredBitRate)
    {
        EnsureEncoder();
        var rates = MediaFoundationEncoder.GetEncodeBitrates(subtype, sampleRate, channels);
        if (rates.Length == 0)
            throw new InvalidOperationException($"No encoder bitrates for {subtype} at {sampleRate} Hz / {channels} ch.");

        return rates
            .OrderBy(rate => Math.Abs(rate - desiredBitRate))
            .ThenByDescending(rate => rate)
            .First();
    }

    private static void Speak(SpeechSynthesizer synth, string language, string script)
    {
        var culture = Culture(language);
        try
        {
            synth.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, culture);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or PlatformNotSupportedException)
        {
            // No voice is installed for this culture. The default voice still reads the script text.
        }

        try
        {
            var prompt = new PromptBuilder(culture);
            prompt.AppendText(script);
            synth.Speak(prompt);
            return;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or PlatformNotSupportedException)
        {
        }

        synth.Speak(script);
    }

    private static CultureInfo Culture(string language) => language switch
    {
        "en" => CultureInfo.GetCultureInfo("en-US"),
        "es" => CultureInfo.GetCultureInfo("es-ES"),
        "de" => CultureInfo.GetCultureInfo("de-DE"),
        "fr" => CultureInfo.GetCultureInfo("fr-FR"),
        "cn" => CultureInfo.GetCultureInfo("zh-CN"),
        _ => CultureInfo.GetCultureInfo("ru-RU")
    };

    /// <summary>
    /// Media Foundation writes an m4a whose <c>uuid</c>, <c>hdlr</c> and <c>udta</c> boxes have odd sizes
    /// and whose media data starts at an unaligned offset. Strict players reject that file.
    /// This keeps the AAC frames and writes moov before mdat, with every box padded to 4 bytes.
    /// </summary>
    private static byte[] ToStandardM4a(byte[] source)
    {
        var moov = FindChild(source, 0, source.Length, "moov"u8);
        var trak = FindChild(source, moov.Start + 8, moov.End, "trak"u8);
        var mdia = FindChild(source, trak.Start + 8, trak.End, "mdia"u8);
        var mdhd = FindChild(source, mdia.Start + 8, mdia.End, "mdhd"u8);
        var minf = FindChild(source, mdia.Start + 8, mdia.End, "minf"u8);
        var stbl = FindChild(source, minf.Start + 8, minf.End, "stbl"u8);
        var stsd = FindChild(source, stbl.Start + 8, stbl.End, "stsd"u8);
        var stts = FindChild(source, stbl.Start + 8, stbl.End, "stts"u8);
        var stsz = FindChild(source, stbl.Start + 8, stbl.End, "stsz"u8);
        var stco = FindChild(source, stbl.Start + 8, stbl.End, "stco"u8);

        if (ReadU32(source, mdhd.Start + 8) != 0)
            throw new InvalidOperationException("Unsupported mdhd version.");
        if (ReadU32(source, stts.Start + 12) != 1)
            throw new InvalidOperationException("AAC file has more than one time-to-sample entry.");
        if (ReadU32(source, stco.Start + 12) != 1)
            throw new InvalidOperationException("AAC file has more than one chunk.");

        var timescale = ReadU32(source, mdhd.Start + 20);
        var duration = ReadU32(source, mdhd.Start + 24);
        var sampleCount = ReadU32(source, stts.Start + 16);
        var sampleDelta = ReadU32(source, stts.Start + 20);
        var sizes = ReadSampleSizes(source, stsz);
        if (sizes.Length != sampleCount)
            throw new InvalidOperationException("AAC sample count does not match the sample table.");

        var sampleOffset = (int)ReadU32(source, stco.Start + 16);
        var payloadLength = 0;
        foreach (var size in sizes)
            payloadLength += size;
        if (sampleOffset < 0 || payloadLength < 0 || sampleOffset + payloadLength > source.Length)
            throw new InvalidOperationException("AAC sample offsets fall outside the file.");

        var payload = new byte[payloadLength];
        var cursor = sampleOffset;
        var write = 0;
        foreach (var size in sizes)
        {
            Buffer.BlockCopy(source, cursor, payload, write, size);
            cursor += size;
            write += size;
        }

        var stsdBox = source.AsSpan(stsd.Start, stsd.Size).ToArray();
        var ftyp = Box("ftyp"u8, "M4A \0\0\0\0M4A mp42isom"u8.ToArray());
        var mvhd = Box("mvhd"u8, MovieHeader(timescale, duration));
        var tkhd = Box("tkhd"u8, TrackHeader(duration));
        var mdhdBox = Box("mdhd"u8, MediaHeader(timescale, duration));
        var hdlr = Box("hdlr"u8, Handler());
        var smhd = Box("smhd"u8, new byte[8]);
        var dinf = Box("dinf"u8, Box("dref"u8, Concat(new byte[4], U32(1), Box("url "u8, new byte[] { 0, 0, 0, 1 }))));
        var sttsBox = Box("stts"u8, Concat(new byte[4], U32(1), U32(sampleCount), U32(sampleDelta)));
        var stsc = Box("stsc"u8, Concat(new byte[4], U32(1), U32(1), U32(sampleCount), U32(1)));
        var stszBox = Box("stsz"u8, SampleSizeBox(sizes));
        var stcoBox = Box("stco"u8, Concat(new byte[4], U32(1), U32(0)));
        var stblBox = Box("stbl"u8, Concat(stsdBox, sttsBox, stsc, stszBox, stcoBox));
        var minfBox = Box("minf"u8, Concat(smhd, dinf, stblBox));
        var mdiaBox = Box("mdia"u8, Concat(mdhdBox, hdlr, minfBox));
        var trakBox = Box("trak"u8, Concat(tkhd, mdiaBox));
        var moovBox = Box("moov"u8, Concat(mvhd, trakBox));

        var pad = (4 - (payload.Length % 4)) % 4;
        var mdat = Box("mdat"u8, pad == 0 ? payload : Concat(payload, new byte[pad]));
        var chunkOffset = ftyp.Length + moovBox.Length + 8;
        var stcoType = moovBox.AsSpan().IndexOf("stco"u8);
        BinaryPrimitives.WriteUInt32BigEndian(moovBox.AsSpan(stcoType + 12, 4), (uint)chunkOffset);

        var file = Concat(ftyp, moovBox, mdat);
        RequireAligned(file);
        return file;
    }

    private static int[] ReadSampleSizes(byte[] source, Mp4Box stsz)
    {
        var constant = ReadU32(source, stsz.Start + 12);
        var count = (int)ReadU32(source, stsz.Start + 16);
        var sizes = new int[count];
        if (constant != 0)
        {
            Array.Fill(sizes, (int)constant);
            return sizes;
        }

        for (var i = 0; i < count; i++)
            sizes[i] = (int)ReadU32(source, stsz.Start + 20 + i * 4);
        return sizes;
    }

    private static byte[] MovieHeader(uint timescale, uint duration)
    {
        var body = new byte[100];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(12, 4), timescale);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(16, 4), duration);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(20, 4), 0x00010000);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(24, 2), 0x0100);
        WriteMatrix(body.AsSpan(36, 36));
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(96, 4), 2);
        return body;
    }

    private static byte[] TrackHeader(uint duration)
    {
        var body = new byte[84];
        body[3] = 7;
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(12, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(20, 4), duration);
        BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(36, 2), 0x0100);
        WriteMatrix(body.AsSpan(40, 36));
        return body;
    }

    private static byte[] MediaHeader(uint timescale, uint duration)
    {
        var body = new byte[24];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(12, 4), timescale);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(16, 4), duration);
        body[20] = 0x55;
        body[21] = 0xC4;
        return body;
    }

    private static byte[] Handler()
    {
        var body = new byte[40];
        "soun"u8.CopyTo(body.AsSpan(8, 4));
        "SoundHandler"u8.CopyTo(body.AsSpan(24, 12));
        return body;
    }

    private static byte[] SampleSizeBox(int[] sizes)
    {
        var body = new byte[12 + sizes.Length * 4];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(8, 4), (uint)sizes.Length);
        for (var i = 0; i < sizes.Length; i++)
            BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(12 + i * 4, 4), (uint)sizes[i]);
        return body;
    }

    private static void WriteMatrix(Span<byte> matrix)
    {
        BinaryPrimitives.WriteUInt32BigEndian(matrix, 0x00010000);
        BinaryPrimitives.WriteUInt32BigEndian(matrix[16..], 0x00010000);
        BinaryPrimitives.WriteUInt32BigEndian(matrix[32..], 0x40000000);
    }

    private static void RequireAligned(byte[] file)
    {
        var index = 0;
        while (index + 8 <= file.Length)
        {
            var size = (int)ReadU32(file, index);
            if (size < 8 || size % 4 != 0 || index + size > file.Length)
                throw new InvalidOperationException($"m4a box at {index} is not 4-byte aligned.");
            index += size;
        }

        if (index != file.Length)
            throw new InvalidOperationException("m4a boxes do not cover the file.");
    }

    private readonly record struct Mp4Box(int Start, int Size)
    {
        public int End => Start + Size;
    }

    private static Mp4Box FindChild(byte[] data, int start, int end, ReadOnlySpan<byte> type)
    {
        var index = start;
        while (index + 8 <= end)
        {
            var size = (int)ReadU32(data, index);
            if (size < 8 || index + size > end)
                throw new InvalidOperationException("Broken MP4 box.");
            if (data.AsSpan(index + 4, 4).SequenceEqual(type))
                return new Mp4Box(index, size);
            index += size;
        }

        throw new InvalidOperationException($"MP4 is missing {Encoding.ASCII.GetString(type)}.");
    }

    private static byte[] Box(ReadOnlySpan<byte> type, byte[] body)
    {
        var box = new byte[8 + body.Length];
        BinaryPrimitives.WriteInt32BigEndian(box.AsSpan(0, 4), box.Length);
        type.CopyTo(box.AsSpan(4, 4));
        body.CopyTo(box.AsSpan(8));
        if (box.Length % 4 != 0)
            throw new InvalidOperationException($"MP4 box {Encoding.ASCII.GetString(type)} is {box.Length} bytes.");
        return box;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var length = 0;
        foreach (var part in parts)
            length += part.Length;
        var result = new byte[length];
        var index = 0;
        foreach (var part in parts)
        {
            part.CopyTo(result.AsSpan(index));
            index += part.Length;
        }

        return result;
    }

    private static byte[] U32(uint value)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return bytes;
    }

    private static uint ReadU32(byte[] data, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));

    private static void EnsureEncoder()
    {
        if (_encoderReady)
            return;

        lock (EncoderGate)
        {
            if (_encoderReady)
                return;
            MediaFoundationApi.Startup();
            _encoderReady = true;
        }
    }
}

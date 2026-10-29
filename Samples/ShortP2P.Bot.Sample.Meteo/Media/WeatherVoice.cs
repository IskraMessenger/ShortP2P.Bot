using System.Globalization;
using System.Speech.Synthesis;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>
/// Speaks the weather script and encodes AAC in an m4a container.
/// Target bitrate is 80 kbit/s; Windows Media Foundation AAC only exposes discrete rates,
/// so the encoder picks the nearest supported rate after resampling to 44.1 kHz mono.
/// </summary>
internal static class WeatherVoice
{
    public const int TargetBitRate = 80_000;
    public const int OutputSampleRate = 44_100;

    private static readonly object EncoderGate = new();
    private static bool _encoderReady;

    public static Task<byte[]> SynthesizeAsync(string language, string script, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(script))
            throw new ArgumentException("Voice script is empty.", nameof(script));

        return Task.Run(() => Synthesize(language, script), cancellationToken);
    }

    private static byte[] Synthesize(string language, string script)
    {
        using var wav = new MemoryStream();
        using (var synth = new SpeechSynthesizer())
        {
            synth.Rate = 0;
            synth.Volume = 100;
            TrySelectVoice(synth, language);
            synth.SetOutputToWaveStream(wav);
            synth.Speak(script);
        }

        if (wav.Length < 64)
            throw new InvalidOperationException("Speech synthesis produced an empty wave.");

        wav.Position = 0;
        var dir = Path.Combine(Path.GetTempPath(), "meteo-voice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var m4aPath = Path.Combine(dir, "weather.m4a");
        try
        {
            EnsureEncoder();
            using var reader = new WaveFileReader(wav);
            var target = new WaveFormat(OutputSampleRate, 16, 1);
            using var resampler = new MediaFoundationResampler(reader, target) { ResamplerQuality = 60 };
            var bitRate = NearestAacBitRate(target.SampleRate, target.Channels, TargetBitRate);
            MediaFoundationEncoder.EncodeToAac(resampler, m4aPath, bitRate);
            var bytes = File.ReadAllBytes(m4aPath);
            if (bytes.Length < 32)
                throw new InvalidOperationException("AAC encoder produced an empty file.");
            return bytes;
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

    /// <summary>Nearest Media Foundation AAC bitrate for the given PCM layout (exact 80 kbit/s is usually unavailable).</summary>
    public static int NearestAacBitRate(int sampleRate, int channels, int desiredBitRate)
    {
        var rates = MediaFoundationEncoder.GetEncodeBitrates(AudioSubtypes.MFAudioFormat_AAC, sampleRate, channels);
        if (rates.Length == 0)
            throw new InvalidOperationException($"No AAC bitrates for {sampleRate} Hz / {channels} ch.");

        return rates
            .OrderBy(rate => Math.Abs(rate - desiredBitRate))
            .ThenByDescending(rate => rate)
            .First();
    }

    private static void TrySelectVoice(SpeechSynthesizer synth, string language)
    {
        var culture = Culture(language);
        var wanted = culture.TwoLetterISOLanguageName;
        try
        {
            var match = synth.GetInstalledVoices()
                .Where(voice => voice.Enabled)
                .Select(voice => voice.VoiceInfo)
                .FirstOrDefault(info =>
                    info.Culture.TwoLetterISOLanguageName.Equals(wanted, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                synth.SelectVoice(match.Name);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or PlatformNotSupportedException)
        {
            // The default voice still reads the script. Pronunciation follows the installed voice.
        }
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

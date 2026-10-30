namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>
/// Voice attachment encoding selected in <c>appsettings</c>.
/// <c>ogg</c> is Opus in an Ogg container, <c>m4a</c> is AAC, and <c>mp3</c> uses Windows Media Foundation.
/// AAC and MP3 only expose discrete rates, so those formats use the nearest supported rate.
/// </summary>
/// <remarks>
/// TTS uses local Windows SAPI5 via <c>System.Speech</c>. Set <see cref="Engine"/> to prefer
/// RHVoice voices when they are installed (see <c>Media/RHVoice.md</c>). Missing RHVoice
/// always falls back to the built-in System.Speech voices so the bot still builds and runs.
/// </remarks>
internal sealed class VoiceOptions
{
    public const string SectionName = "Voice";

    public const string DefaultFormat = "ogg";

    public const string DefaultEngine = "Auto";

    public const int DefaultBitRate = 64_000;

    public const int MinBitRate = 8_000;

    public const int MaxBitRate = 320_000;

    /// <summary><c>ogg</c>, <c>m4a</c>, or <c>mp3</c>. Empty means ogg.</summary>
    public string Format { get; set; } = DefaultFormat;

    /// <summary>Target bitrate in bits per second.</summary>
    public int BitRate { get; set; } = DefaultBitRate;

    /// <summary>
    /// <c>Auto</c> prefers RHVoice when a matching SAPI voice is installed, otherwise System.Speech.
    /// <c>RHVoice</c> prefers RHVoice and falls back if none match.
    /// <c>SystemSpeech</c> skips RHVoice and uses Microsoft/other SAPI voices.
    /// </summary>
    public string Engine { get; set; } = DefaultEngine;

    /// <summary>
    /// Optional SAPI voice name (exact or substring), e.g. <c>Elena</c> or <c>RHVoice Elena</c>.
    /// Applied after language matching when the engine allows that voice.
    /// </summary>
    public string? PreferredVoice { get; set; }

    public VoiceFormat SelectedFormat => Parse(Format);

    public VoiceEngine SelectedEngine => ParseEngine(Engine);

    public void Validate()
    {
        Parse(Format);
        RequireBitRate(BitRate);
        ParseEngine(Engine);
    }

    public static VoiceFormat Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Trim().Equals("ogg", StringComparison.OrdinalIgnoreCase))
            return VoiceFormat.Ogg;

        if (value.Trim().Equals("m4a", StringComparison.OrdinalIgnoreCase))
            return VoiceFormat.M4a;

        if (value.Trim().Equals("mp3", StringComparison.OrdinalIgnoreCase))
            return VoiceFormat.Mp3;

        throw new InvalidOperationException(
            $"Voice:Format '{value.Trim()}' is unknown. Use ogg, m4a, or mp3.");
    }

    public static VoiceEngine ParseEngine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Trim().Equals("Auto", StringComparison.OrdinalIgnoreCase))
            return VoiceEngine.Auto;

        if (value.Trim().Equals("SystemSpeech", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("System.Speech", StringComparison.OrdinalIgnoreCase))
            return VoiceEngine.SystemSpeech;

        if (value.Trim().Equals("RHVoice", StringComparison.OrdinalIgnoreCase))
            return VoiceEngine.RHVoice;

        throw new InvalidOperationException(
            $"Voice:Engine '{value.Trim()}' is unknown. Use Auto, SystemSpeech, or RHVoice.");
    }

    public static int RequireBitRate(int bitRate)
    {
        if (bitRate is < MinBitRate or > MaxBitRate)
            throw new InvalidOperationException(
                $"Voice:BitRate must be between {MinBitRate} and {MaxBitRate} bits per second.");

        return bitRate;
    }
}

internal enum VoiceFormat
{
    Ogg,
    M4a,
    Mp3
}

/// <summary>Local TTS engine preference for SAPI voice selection.</summary>
internal enum VoiceEngine
{
    /// <summary>Prefer RHVoice when present for the language; otherwise System.Speech.</summary>
    Auto,

    /// <summary>Use non-RHVoice SAPI voices (typically Microsoft).</summary>
    SystemSpeech,

    /// <summary>Prefer RHVoice; fall back to System.Speech if none match.</summary>
    RHVoice
}

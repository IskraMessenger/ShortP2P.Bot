namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>
/// Voice attachment encoding selected in <c>appsettings</c>.
/// <c>ogg</c> is Opus in an Ogg container, <c>m4a</c> is AAC, and <c>mp3</c> uses Windows Media Foundation.
/// AAC and MP3 only expose discrete rates, so those formats use the nearest supported rate.
/// </summary>
internal sealed class VoiceOptions
{
    public const string SectionName = "Voice";

    public const string DefaultFormat = "ogg";

    public const int DefaultBitRate = 64_000;

    public const int MinBitRate = 8_000;

    public const int MaxBitRate = 320_000;

    /// <summary><c>ogg</c>, <c>m4a</c>, or <c>mp3</c>. Empty means ogg.</summary>
    public string Format { get; set; } = DefaultFormat;

    /// <summary>Target bitrate in bits per second.</summary>
    public int BitRate { get; set; } = DefaultBitRate;

    public VoiceFormat SelectedFormat => Parse(Format);

    public void Validate()
    {
        Parse(Format);
        RequireBitRate(BitRate);
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

    public static int RequireBitRate(int bitRate)
    {
        if (bitRate < MinBitRate || bitRate > MaxBitRate)
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

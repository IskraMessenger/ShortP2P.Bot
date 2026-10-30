namespace ShortP2P.Bot.Sample.Meteo.Media;

/// <summary>Encoded weather voice and the container it was written in.</summary>
internal readonly record struct VoiceClip(byte[] Bytes, VoiceFormat Format, int BitRate)
{
    public string Extension => Format switch
    {
        VoiceFormat.M4a => ".m4a",
        VoiceFormat.Mp3 => ".mp3",
        _ => ".ogg"
    };

    public string FileName => "weather" + Extension;

    public string MimeType => Format switch
    {
        VoiceFormat.M4a => "audio/mp4",
        VoiceFormat.Mp3 => "audio/mpeg",
        _ => "audio/ogg"
    };
}

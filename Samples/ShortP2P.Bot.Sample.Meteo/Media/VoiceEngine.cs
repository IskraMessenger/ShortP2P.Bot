namespace ShortP2P.Bot.Sample.Meteo.Media;

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
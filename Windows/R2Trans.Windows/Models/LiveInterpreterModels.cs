namespace R2Trans.Windows.Models;

public enum LiveInterpreterInputSource
{
    Microphone,
    SystemAudio,
    MicrophoneAndSystemAudio
}

public static class LiveInterpreterInputSourceExtensions
{
    public static bool IncludesMicrophone(this LiveInterpreterInputSource source) => source is
        LiveInterpreterInputSource.Microphone or LiveInterpreterInputSource.MicrophoneAndSystemAudio;

    public static bool IncludesSystemAudio(this LiveInterpreterInputSource source) => source is
        LiveInterpreterInputSource.SystemAudio or LiveInterpreterInputSource.MicrophoneAndSystemAudio;
}

public enum LiveInterpreterAudioSource
{
    Microphone,
    SystemAudio
}

public sealed record RealtimeTranslationLanguage(string Code, string DisplayName)
{
    public string ApiLanguageCode => Code.Split('-').FirstOrDefault()?.ToLowerInvariant() ?? Code.ToLowerInvariant();
}

public abstract record LiveInterpreterUpdate
{
    public sealed record RunningStateChanged(bool IsRunning) : LiveInterpreterUpdate;
    public sealed record Status(string Message) : LiveInterpreterUpdate;
    public sealed record SourceTranscript(string Text) : LiveInterpreterUpdate;
    public sealed record Subtitle(string Text, string LanguageLabel) : LiveInterpreterUpdate;
    public sealed record AudioLevel(LiveInterpreterAudioSource Source, double Level) : LiveInterpreterUpdate;
    public sealed record Debug(string Message) : LiveInterpreterUpdate;
    public sealed record Error(string Message) : LiveInterpreterUpdate;
}

public abstract record RealtimeTranslationEvent
{
    public sealed record InputTranscriptDelta(string Delta) : RealtimeTranslationEvent;
    public sealed record OutputTranscriptDelta(string Delta) : RealtimeTranslationEvent;
    public sealed record Status(string Message) : RealtimeTranslationEvent;
    public sealed record Debug(string Message) : RealtimeTranslationEvent;
    public sealed record Error(string Message) : RealtimeTranslationEvent;
}

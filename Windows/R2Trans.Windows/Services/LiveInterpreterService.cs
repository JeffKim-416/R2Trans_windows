using System.Collections.Concurrent;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class LiveInterpreterService : IDisposable
{
    private readonly SecureCredentialStore credentialStore;
    private readonly AudioInputStreamer audioInputStreamer = new();
    private readonly ConcurrentDictionary<LiveInterpreterAudioSource, DateTimeOffset> lastAudioLevelUpdate = new();
    private RealtimeTranslationSocket? translationSocket;
    private string sourceTranscript = string.Empty;
    private string translatedSubtitle = string.Empty;
    private string targetLanguageDisplayName = string.Empty;
    private int audioChunkCount;

    public LiveInterpreterService(SecureCredentialStore credentialStore)
    {
        this.credentialStore = credentialStore;
    }

    public event EventHandler<LiveInterpreterUpdate>? Update;

    public bool IsRunning { get; private set; }

    public async Task StartAsync(LiveInterpreterInputSource inputSource, string targetLanguageCode)
    {
        if (IsRunning)
        {
            return;
        }

        var apiKey = credentialStore.LoadApiKey().Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new R2TransException(AppText.Text(TextKey.APIKeyMissing));
        }

        ResetTranscriptState();
        targetLanguageDisplayName = SupportedLanguage.DisplayNameFor(targetLanguageCode);
        var targetLanguage = new RealtimeTranslationLanguage(targetLanguageCode, targetLanguageDisplayName);
        translationSocket = new RealtimeTranslationSocket(targetLanguage);
        translationSocket.EventReceived += (_, evt) => Handle(evt);
        translationSocket.Error += (_, message) => SendUpdate(new LiveInterpreterUpdate.Error(message));

        SendUpdate(new LiveInterpreterUpdate.Status(AppText.Text(TextKey.LiveInterpreterConnecting)));
        await translationSocket.ConnectAsync(apiKey);

        try
        {
            audioInputStreamer.Start(inputSource, SendAudio);
        }
        catch
        {
            Stop();
            throw;
        }

        IsRunning = true;
        SendUpdate(new LiveInterpreterUpdate.RunningStateChanged(true));
        SendUpdate(new LiveInterpreterUpdate.Status(AppText.Text(TextKey.LiveInterpreterListening)));
    }

    public void Stop()
    {
        audioInputStreamer.Stop();
        translationSocket?.Dispose();
        translationSocket = null;
        SendUpdate(new LiveInterpreterUpdate.AudioLevel(LiveInterpreterAudioSource.Microphone, 0));
        SendUpdate(new LiveInterpreterUpdate.AudioLevel(LiveInterpreterAudioSource.SystemAudio, 0));

        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        SendUpdate(new LiveInterpreterUpdate.RunningStateChanged(false));
        SendUpdate(new LiveInterpreterUpdate.Status(AppText.Text(TextKey.LiveInterpreterStopped)));
    }

    public void Clear()
    {
        ResetTranscriptState();
        SendUpdate(new LiveInterpreterUpdate.SourceTranscript(string.Empty));
        SendUpdate(new LiveInterpreterUpdate.Subtitle(string.Empty, string.Empty));
        SendUpdate(new LiveInterpreterUpdate.AudioLevel(LiveInterpreterAudioSource.Microphone, 0));
        SendUpdate(new LiveInterpreterUpdate.AudioLevel(LiveInterpreterAudioSource.SystemAudio, 0));
    }

    private void SendAudio(AudioChunk chunk)
    {
        PublishAudioLevelIfNeeded(chunk);
        PublishAudioChunkDebugIfNeeded();
        _ = translationSocket?.SendAudioAsync(Convert.ToBase64String(chunk.Data));
    }

    private void PublishAudioChunkDebugIfNeeded()
    {
        var count = Interlocked.Increment(ref audioChunkCount);
        if (count == 1 || count % 120 == 0)
        {
            SendUpdate(new LiveInterpreterUpdate.Debug($"audio chunks sent: {count}"));
        }
    }

    private void PublishAudioLevelIfNeeded(AudioChunk chunk)
    {
        var now = DateTimeOffset.UtcNow;
        var lastUpdate = lastAudioLevelUpdate.GetValueOrDefault(chunk.Source);
        if (now - lastUpdate < TimeSpan.FromMilliseconds(80))
        {
            return;
        }

        lastAudioLevelUpdate[chunk.Source] = now;
        SendUpdate(new LiveInterpreterUpdate.AudioLevel(chunk.Source, AudioLevel(chunk.Data)));
    }

    private void Handle(RealtimeTranslationEvent evt)
    {
        switch (evt)
        {
            case RealtimeTranslationEvent.InputTranscriptDelta inputDelta:
                sourceTranscript = TrimmedTail(sourceTranscript + inputDelta.Delta, 800);
                SendUpdate(new LiveInterpreterUpdate.SourceTranscript(LineBrokenSentences(sourceTranscript)));
                break;
            case RealtimeTranslationEvent.OutputTranscriptDelta outputDelta:
                translatedSubtitle = TrimmedTail(translatedSubtitle + outputDelta.Delta, 1500);
                SendUpdate(new LiveInterpreterUpdate.Subtitle(LineBrokenSentences(translatedSubtitle), targetLanguageDisplayName));
                break;
            case RealtimeTranslationEvent.Status status:
                SendUpdate(new LiveInterpreterUpdate.Status(status.Message));
                break;
            case RealtimeTranslationEvent.Debug debug:
                SendUpdate(new LiveInterpreterUpdate.Debug(debug.Message));
                break;
            case RealtimeTranslationEvent.Error error:
                SendUpdate(new LiveInterpreterUpdate.Error(error.Message));
                break;
        }
    }

    private void ResetTranscriptState()
    {
        sourceTranscript = string.Empty;
        translatedSubtitle = string.Empty;
        targetLanguageDisplayName = string.Empty;
        audioChunkCount = 0;
        lastAudioLevelUpdate.Clear();
    }

    private void SendUpdate(LiveInterpreterUpdate update)
    {
        Update?.Invoke(this, update);
    }

    private static string TrimmedTail(string value, int limit) => value.Length > limit ? value[^limit..] : value;

    private static string LineBrokenSentences(string text)
    {
        var result = new System.Text.StringBuilder();
        var previousWasLineBreak = false;
        var terminators = new HashSet<char> { '.', '!', '?', '。', '！', '？' };

        foreach (var character in text)
        {
            if (character == '\n')
            {
                if (!previousWasLineBreak)
                {
                    result.Append(character);
                }

                previousWasLineBreak = true;
                continue;
            }

            if (previousWasLineBreak && char.IsWhiteSpace(character))
            {
                continue;
            }

            result.Append(character);
            if (terminators.Contains(character))
            {
                result.Append('\n');
                previousWasLineBreak = true;
            }
            else
            {
                previousWasLineBreak = false;
            }
        }

        return result.ToString().Trim();
    }

    private static double AudioLevel(byte[] data)
    {
        var sampleCount = data.Length / 2;
        if (sampleCount == 0)
        {
            return 0;
        }

        var sumSquares = 0.0;
        for (var index = 0; index < sampleCount * 2; index += 2)
        {
            var sample = BitConverter.ToInt16(data, index);
            sumSquares += sample * sample;
        }

        var rms = Math.Sqrt(sumSquares / sampleCount) / short.MaxValue;
        return Math.Min(1, rms * 8);
    }

    public void Dispose()
    {
        Stop();
        audioInputStreamer.Dispose();
    }
}

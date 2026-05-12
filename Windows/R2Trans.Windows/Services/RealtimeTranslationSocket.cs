using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class RealtimeTranslationSocket : IDisposable
{
    private readonly RealtimeTranslationLanguage targetLanguage;
    private readonly ClientWebSocket socket = new();
    private readonly SemaphoreSlim sendLock = new(1, 1);
    private CancellationTokenSource? receiveCancellation;
    private bool disposed;

    public RealtimeTranslationSocket(RealtimeTranslationLanguage targetLanguage)
    {
        this.targetLanguage = targetLanguage;
    }

    public event EventHandler<RealtimeTranslationEvent>? EventReceived;
    public event EventHandler<string>? Error;

    public async Task ConnectAsync(string apiKey)
    {
        socket.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        socket.Options.SetRequestHeader("OpenAI-Safety-Identifier", "r2trans-local-user");
        await socket.ConnectAsync(new Uri("wss://api.openai.com/v1/realtime/translations?model=gpt-realtime-translate"), CancellationToken.None);
        EventReceived?.Invoke(this, new RealtimeTranslationEvent.Debug($"socket started: {targetLanguage.ApiLanguageCode}"));
        await SendSessionUpdateAsync();

        receiveCancellation = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoopAsync(receiveCancellation.Token));
    }

    public Task SendAudioAsync(string base64Audio)
    {
        return SendJsonAsync(new Dictionary<string, object?>
        {
            ["type"] = "session.input_audio_buffer.append",
            ["audio"] = base64Audio
        });
    }

    private Task SendSessionUpdateAsync()
    {
        return SendJsonAsync(new Dictionary<string, object?>
        {
            ["type"] = "session.update",
            ["session"] = new Dictionary<string, object?>
            {
                ["audio"] = new Dictionary<string, object?>
                {
                    ["input"] = new Dictionary<string, object?>
                    {
                        ["transcription"] = new Dictionary<string, object?>
                        {
                            ["model"] = "gpt-realtime-whisper"
                        },
                        ["noise_reduction"] = new Dictionary<string, object?>
                        {
                            ["type"] = "near_field"
                        }
                    },
                    ["output"] = new Dictionary<string, object?>
                    {
                        ["language"] = targetLanguage.ApiLanguageCode
                    }
                }
            }
        });
    }

    private async Task SendJsonAsync(Dictionary<string, object?> payload)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        await sendLock.WaitAsync();
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception exception)
        {
            Error?.Invoke(this, exception.Message);
        }
        finally
        {
            sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            try
            {
                using var memory = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    memory.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                HandleMessage(Encoding.UTF8.GetString(memory.ToArray()));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Error?.Invoke(this, exception.Message);
                return;
            }
        }
    }

    private void HandleMessage(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("type", out var typeElement))
            {
                return;
            }

            var type = typeElement.GetString() ?? string.Empty;
            switch (type)
            {
                case "session.input_transcript.delta":
                    PublishDelta(document, "delta", delta => new RealtimeTranslationEvent.InputTranscriptDelta(delta));
                    break;
                case "session.output_transcript.delta":
                    PublishDelta(document, "delta", delta => new RealtimeTranslationEvent.OutputTranscriptDelta(delta));
                    break;
                case "session.created":
                case "session.updated":
                    EventReceived?.Invoke(this, new RealtimeTranslationEvent.Debug(type));
                    break;
                case "error":
                case "session.error":
                    EventReceived?.Invoke(this, new RealtimeTranslationEvent.Error(ErrorMessage(document.RootElement)));
                    break;
                default:
                    EventReceived?.Invoke(this, type.Contains("error", StringComparison.OrdinalIgnoreCase)
                        ? new RealtimeTranslationEvent.Error(ErrorMessage(document.RootElement))
                        : new RealtimeTranslationEvent.Debug(type));
                    break;
            }
        }
        catch (JsonException)
        {
        }
    }

    private void PublishDelta(JsonDocument document, string propertyName, Func<string, RealtimeTranslationEvent> factory)
    {
        if (document.RootElement.TryGetProperty(propertyName, out var delta)
            && delta.ValueKind == JsonValueKind.String)
        {
            EventReceived?.Invoke(this, factory(delta.GetString() ?? string.Empty));
        }
    }

    private static string ErrorMessage(JsonElement root)
    {
        if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
        {
            return message.GetString() ?? "Realtime translation failed.";
        }

        if (root.TryGetProperty("error", out var error)
            && error.TryGetProperty("message", out var errorMessage)
            && errorMessage.ValueKind == JsonValueKind.String)
        {
            return errorMessage.GetString() ?? "Realtime translation failed.";
        }

        return "Realtime translation failed.";
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        receiveCancellation?.Cancel();
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            _ = socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "R2Trans closing", CancellationToken.None);
        }

        socket.Dispose();
        sendLock.Dispose();
        receiveCancellation?.Dispose();
    }
}

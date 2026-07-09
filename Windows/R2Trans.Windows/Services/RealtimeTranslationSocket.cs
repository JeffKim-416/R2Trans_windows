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
    private readonly TaskCompletionSource sessionClosed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object closeLock = new();
    private CancellationTokenSource? receiveCancellation;
    private Task? receiveTask;
    private Task? closeTask;
    private bool closing;
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
        receiveTask = Task.Run(() => ReceiveLoopAsync(receiveCancellation.Token));
    }

    public Task SendAudioAsync(string base64Audio)
    {
        if (closing)
        {
            return Task.CompletedTask;
        }

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
                    ["output"] = new Dictionary<string, object?>
                    {
                        ["language"] = targetLanguage.ApiLanguageCode
                    }
                }
            }
        });
    }

    public Task CloseAsync()
    {
        lock (closeLock)
        {
            closeTask ??= CloseCoreAsync();
            return closeTask;
        }
    }

    private async Task CloseCoreAsync()
    {
        if (disposed)
        {
            return;
        }

        closing = true;

        if (socket.State == WebSocketState.Open)
        {
            await SendJsonAsync(new Dictionary<string, object?>
            {
                ["type"] = "session.close"
            }).ConfigureAwait(false);

            await Task.WhenAny(sessionClosed.Task, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
        }

        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "R2Trans closing", CancellationToken.None).ConfigureAwait(false);
            }
            catch (WebSocketException exception)
            {
                Error?.Invoke(this, exception.Message);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        receiveCancellation?.Cancel();
        if (receiveTask is not null)
        {
            await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(1))).ConfigureAwait(false);
        }
    }

    private async Task SendJsonAsync(Dictionary<string, object?> payload)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        await sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
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
                        sessionClosed.TrySetResult();
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
            catch (Exception) when (closing)
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
                case "session.closed":
                    sessionClosed.TrySetResult();
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

        try
        {
            CloseAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        disposed = true;
        socket.Dispose();
        sendLock.Dispose();
        receiveCancellation?.Dispose();
    }
}

using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed record AudioChunk(LiveInterpreterAudioSource Source, byte[] Data);

public sealed class AudioInputStreamer : IDisposable
{
    private readonly List<IDisposable> activeCaptures = [];
    private Action<AudioChunk>? onAudioData;

    public void Start(LiveInterpreterInputSource inputSource, Action<AudioChunk> onAudioData)
    {
        Stop();
        this.onAudioData = onAudioData;

        if (inputSource.IncludesMicrophone())
        {
            StartMicrophone();
        }

        if (inputSource.IncludesSystemAudio())
        {
            StartSystemAudio();
        }
    }

    public void Stop()
    {
        foreach (var capture in activeCaptures)
        {
            capture.Dispose();
        }

        activeCaptures.Clear();
        onAudioData = null;
    }

    private void StartMicrophone()
    {
        if (WaveInEvent.DeviceCount <= 0)
        {
            throw new R2TransException(AppText.Text(TextKey.MicrophoneUnavailable));
        }

        var capture = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(24_000, 16, 1),
            BufferMilliseconds = 80
        };

        capture.DataAvailable += (_, args) =>
        {
            var bytes = args.Buffer.Take(args.BytesRecorded).ToArray();
            onAudioData?.Invoke(new AudioChunk(LiveInterpreterAudioSource.Microphone, bytes));
        };
        capture.StartRecording();
        activeCaptures.Add(new CaptureHandle(capture.StopRecording, capture.Dispose));
    }

    private void StartSystemAudio()
    {
        try
        {
            var capture = new WasapiLoopbackCapture();
            capture.DataAvailable += (_, args) =>
            {
                var bytes = ConvertToPcm16Mono24k(args.Buffer, args.BytesRecorded, capture.WaveFormat);
                if (bytes.Length > 0)
                {
                    onAudioData?.Invoke(new AudioChunk(LiveInterpreterAudioSource.SystemAudio, bytes));
                }
            };
            capture.StartRecording();
            activeCaptures.Add(new CaptureHandle(capture.StopRecording, capture.Dispose));
        }
        catch (Exception exception)
        {
            throw new R2TransException(AppText.Text(TextKey.SystemAudioUnavailable), exception);
        }
    }

    private static byte[] ConvertToPcm16Mono24k(byte[] buffer, int bytesRecorded, WaveFormat inputFormat)
    {
        using var sourceStream = new RawSourceWaveStream(new MemoryStream(buffer, 0, bytesRecorded), inputFormat);
        ISampleProvider sampleProvider = sourceStream.ToSampleProvider();

        if (sampleProvider.WaveFormat.Channels > 1)
        {
            sampleProvider = new StereoToMonoSampleProvider(sampleProvider)
            {
                LeftVolume = 0.5f,
                RightVolume = 0.5f
            };
        }

        if (sampleProvider.WaveFormat.SampleRate != 24_000)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, 24_000);
        }

        var waveProvider = new SampleToWaveProvider16(sampleProvider);
        var output = new byte[4096];
        using var memory = new MemoryStream();
        int read;
        while ((read = waveProvider.Read(output, 0, output.Length)) > 0)
        {
            memory.Write(output, 0, read);
        }

        return memory.ToArray();
    }

    public void Dispose()
    {
        Stop();
    }

    private sealed class CaptureHandle : IDisposable
    {
        private readonly Action stop;
        private readonly Action dispose;
        private bool disposed;

        public CaptureHandle(Action stop, Action dispose)
        {
            this.stop = stop;
            this.dispose = dispose;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            try
            {
                stop();
            }
            catch
            {
            }

            dispose();
        }
    }
}

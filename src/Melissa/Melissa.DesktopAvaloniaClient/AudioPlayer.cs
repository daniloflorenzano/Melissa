using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PortAudioSharp;

namespace Melissa.DesktopAvaloniaClient;

public class AudioPlayer
{
    private readonly Stream.Callback _outputCallback;
    private Stream? _outputStream;
    private readonly Queue<byte[]> _playbackQueue = new();
    private readonly object _queueLock = new();

    public AudioPlayer()
    {
        _outputCallback = OutputCallbackImpl;
    }

    public void StartPlayback()
    {
        PortAudio.Initialize();

        var outputParam = new StreamParameters
        {
            device = PortAudio.DefaultOutputDevice,
            channelCount = 1,
            sampleFormat = SampleFormat.Int16,
            suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice).defaultLowOutputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero
        };

        const int sampleRate = 16000;
        const uint framesPerBuffer = 256u;

        _outputStream = new Stream(
            null, outputParam, sampleRate, framesPerBuffer,
            StreamFlags.ClipOff, _outputCallback, IntPtr.Zero
        );
        _outputStream.Start();
        Console.WriteLine("[INFO] Reprodução de áudio iniciada.");
    }

    public void AddAudioData(byte[] audioData)
    {
        lock (_queueLock)
        {
            _playbackQueue.Enqueue(audioData);
        }
    }

    public void StopPlayback()
    {
        if (_outputStream != null)
        {
            _outputStream.Stop();
            _outputStream.Dispose();
            _outputStream = null;
        }

        PortAudio.Terminate();
        Console.WriteLine("[INFO] Reprodução de áudio encerrada.");
    }

    private StreamCallbackResult OutputCallbackImpl(
        IntPtr input, IntPtr output, uint frameCount,
        ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
    {
        if (output == IntPtr.Zero)
            return StreamCallbackResult.Continue;

        int bufferSize = (int)(frameCount * sizeof(short));
        byte[]? data = null;

        lock (_queueLock)
        {
            if (_playbackQueue.Count > 0)
            {
                data = _playbackQueue.Dequeue();
            }
        }

        if (data != null)
        {
            int copyLen = Math.Min(bufferSize, data.Length);
            Marshal.Copy(data, 0, output, copyLen);

            if (copyLen < bufferSize)
            {
                Span<byte> silence = stackalloc byte[bufferSize - copyLen];
                Marshal.Copy(silence.ToArray(), 0, output + copyLen, bufferSize - copyLen);
            }
        }
        else
        {
            Span<byte> silence = stackalloc byte[bufferSize];
            Marshal.Copy(silence.ToArray(), 0, output, bufferSize);
        }

        return StreamCallbackResult.Continue;
    }
}
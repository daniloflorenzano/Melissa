using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using PortAudioSharp;
using LibVLCSharp.Shared;

namespace Melissa.DesktopAvaloniaClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Fale com a Melissa!";
    private const string MelissaServerUrl = "http://localhost:5179";

    public MainWindowViewModel()
    {
        _callback = CallbackImpl;
        Core.Initialize();
        _libVLC = new LibVLC();
    }

    private HubConnection? _hubConnection;
    private Channel<byte[]>? _audioChannel;
    private GCHandle? _audioChannelHandle;
    private readonly PortAudioSharp.Stream.Callback _callback;
    private PortAudioSharp.Stream? _audioStream;
    private LibVLC _libVLC;

    [RelayCommand]
    public async Task StartAudioCaptureAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{MelissaServerUrl}/melissa")
            .Build();

        await _hubConnection.StartAsync();

        _audioChannel = Channel.CreateUnbounded<byte[]>();
        Console.WriteLine($"Canal criado: {_audioChannel.GetHashCode()}");

        // Inicia o envio dos blocos para o Hub
        _ = Task.Run(async () =>
        {
            var stream = _hubConnection.StreamAsync<byte[]>(
                "AskMelissaAudio",
                GetAudioStream(),
                CancellationToken.None
            );

            await foreach (var replyBytes in stream)
            {
                var wavBytes = AddWavHeader(replyBytes);
                using var ms = new MemoryStream(wavBytes);
                using var mediaInput = new StreamMediaInput(ms);
                using var media = new Media(_libVLC, mediaInput);
                using var mediaPlayer = new MediaPlayer(media);
                mediaPlayer.Play();
            }
        });

        PortAudio.Initialize();
        StartStream();
    }
    
    private static byte[] AddWavHeader(byte[] pcmData, int sampleRate = 16000, short channels = 1, short bitsPerSample = 16)
    {
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = (short)(channels * bitsPerSample / 8);
        int subChunk2Size = pcmData.Length;
        int chunkSize = 36 + subChunk2Size;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt subchunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size for PCM
        bw.Write((short)1); // AudioFormat PCM
        bw.Write(channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write(bitsPerSample);

        // data subchunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(subChunk2Size);
        bw.Write(pcmData);

        return ms.ToArray();
    }

    private async IAsyncEnumerable<byte[]> GetAudioStream()
    {
        while (_audioChannel != null && await _audioChannel.Reader.WaitToReadAsync())
        {
            while (_audioChannel.Reader.TryRead(out var buffer))
            {
                yield return buffer;
            }
        }
    }

    private StreamCallbackResult CallbackImpl(
        IntPtr input, IntPtr output, uint frameCount,
        ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
    {
        int bufferSize = (int)(frameCount * sizeof(short));
        byte[] audioBuffer = new byte[bufferSize];
        if (input != IntPtr.Zero)
        {
            Marshal.Copy(input, audioBuffer, 0, bufferSize);
            _audioChannel?.Writer.TryWrite(audioBuffer);
        }
        return StreamCallbackResult.Continue;
    }

    private void StartStream()
    {
        // for (int i = 0; i < PortAudio.DeviceCount; i++)
        // {
        //     var info = PortAudio.GetDeviceInfo(i);
        //     Console.WriteLine($"Device {i}: {info.name}, maxInputChannels={info.maxInputChannels}");
        // }
        //
        // Console.WriteLine($"DefaultInputDevice: {PortAudio.DefaultInputDevice}");
        // var defaultInfo = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice);
        // Console.WriteLine($"Default device name: {defaultInfo.name}, maxInputChannels={defaultInfo.maxInputChannels}");
        
        var param = new StreamParameters
        {
            device = 22, // PortAudio.DefaultInputDevice,
            channelCount = 1,
            sampleFormat = SampleFormat.Int16,
            suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice).defaultLowInputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero
        };

        const int sampleRate = 16000;
        const uint framesPerBuffer = 256u;

        // Libera handle anterior, se existir
        if (_audioChannelHandle.HasValue && _audioChannelHandle.Value.IsAllocated)
            _audioChannelHandle.Value.Free();

        Console.WriteLine($"Criando GCHandle para canal: {_audioChannel?.GetHashCode()}");
        _audioChannelHandle = GCHandle.Alloc(_audioChannel!, GCHandleType.Normal);

        _audioStream = new PortAudioSharp.Stream(
            param, null, sampleRate, framesPerBuffer,
            StreamFlags.ClipOff, _callback, IntPtr.Zero // nao usa ponteiro para userData
        );
        _audioStream.Start();
    }

    public void StopAudioCapture()
    {
        // Completa o canal para encerrar o envio de áudio
        _audioChannel?.Writer.Complete();

        // Para e libera o stream do PortAudio, se existir
        if (_audioStream != null)
        {
            _audioStream.Stop();
            _audioStream.Dispose();
            _audioStream = null;
        }

        // Libera o GCHandle
        if (_audioChannelHandle.HasValue && _audioChannelHandle.Value.IsAllocated)
        {
            _audioChannelHandle.Value.Free();
            _audioChannelHandle = null;
        }
    }
}
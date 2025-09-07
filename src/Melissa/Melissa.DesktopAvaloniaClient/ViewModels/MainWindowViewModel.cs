using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using PortAudioSharp;
using Stream = PortAudioSharp.Stream;

namespace Melissa.DesktopAvaloniaClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string MelissaServerUrl = "http://localhost:5179";

    private HubConnection? _hubConnection;
    private Channel<byte[]>? _audioChannel;
    private readonly Stream.Callback _inputCallback;
    private Stream? _inputStream;

    public MainWindowViewModel()
    {
        _inputCallback = InputCallbackImpl;
    }

    [RelayCommand]
    public async Task StartAudioCaptureAsync()
    {
        Console.WriteLine("[INFO] Iniciando captura de áudio e conexão com servidor...");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{MelissaServerUrl}/melissa")
            .Build();

        await _hubConnection.StartAsync();
        Console.WriteLine("[INFO] Conexão com SignalR iniciada.");

        _audioChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        //var audioPlayer = new AudioPlayer();
        var receivedAudioFile = Path.Combine(Path.GetTempPath(), "received_audio.wav");
        

        // Inicia task de envio/recepção
        _ = Task.Run(async () =>
        {
            Console.WriteLine("[INFO] Iniciando task de envio/recepção de áudio...");
            var stream = _hubConnection.StreamAsync<byte[]>(
                "AskMelissaAudio",
                GetAudioStream(),
                CancellationToken.None
            );

            await foreach (var replyBytes in stream)
            {
                Console.WriteLine($"[RECV] Recebido {replyBytes.Length} bytes do servidor.");
                
                await using var fileStream = new FileStream(receivedAudioFile, FileMode.Create, FileAccess.Write);
                await fileStream.WriteAsync(replyBytes);
            }

            Console.WriteLine($"[INFO] Áudio recebido salvo em: {receivedAudioFile}");
            
            var player = new NetCoreAudio.Player();
            await player.Play(receivedAudioFile);

            File.Delete(receivedAudioFile);
        });

        PortAudio.Initialize();
        StartInputStream();
    }

    private async IAsyncEnumerable<byte[]> GetAudioStream()
    {
        if (_audioChannel is null) yield break;

        Console.WriteLine("[INFO] Aguardando buffers de áudio para envio...");
        while (await _audioChannel.Reader.WaitToReadAsync())
        {
            while (_audioChannel.Reader.TryRead(out var buffer))
            {
                yield return buffer;
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private StreamCallbackResult InputCallbackImpl(
        IntPtr input, IntPtr output, uint frameCount,
        ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
    {
        if (input == IntPtr.Zero || _audioChannel is null)
            return StreamCallbackResult.Continue;

        var bufferSize = (int)(frameCount * sizeof(short));
        var rented = ArrayPool<byte>.Shared.Rent(bufferSize);

        Marshal.Copy(input, rented, 0, bufferSize);
        _audioChannel.Writer.TryWrite(rented);

        return StreamCallbackResult.Continue;
    }

    private void StartInputStream()
    {
        var inputParam = new StreamParameters
        {
            device = PortAudio.DefaultInputDevice,
            channelCount = 1,
            sampleFormat = SampleFormat.Int16,
            suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice).defaultLowInputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero
        };

        const int sampleRate = 16000;
        const uint framesPerBuffer = 256u;

        _inputStream = new Stream(
            inputParam, null, sampleRate, framesPerBuffer,
            StreamFlags.ClipOff, _inputCallback, IntPtr.Zero
        );
        _inputStream.Start();
        Console.WriteLine("[INFO] Stream de entrada iniciado.");
    }

    public void StopAudioCapture()
    {
        Console.WriteLine("[INFO] Parando captura/reprodução de áudio...");

        _audioChannel?.Writer.Complete();

        if (_inputStream != null)
        {
            _inputStream.Stop();
            _inputStream.Dispose();
            _inputStream = null;
        }

        Console.WriteLine("[INFO] Captura/reprodução encerradas.");
    }
}
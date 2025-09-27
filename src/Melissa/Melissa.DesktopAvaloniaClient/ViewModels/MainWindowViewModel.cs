using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melissa.DesktopAvaloniaClient.Views;
using Microsoft.AspNetCore.SignalR.Client;
using PortAudioSharp;
using Stream = PortAudioSharp.Stream;

namespace Melissa.DesktopAvaloniaClient.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _melissaServerUrl;
    [ObservableProperty] private bool _isPlayingAudio;
    [ObservableProperty] private List<short> _audioWaveformData = [];
    [ObservableProperty] private string _connectionStatus;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _serverStatusColor;

    private HubConnection? _hubConnection;
    private Channel<byte[]>? _audioChannel;
    private readonly Stream.Callback _inputCallback;
    private Stream? _inputStream;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private CancellationTokenSource? _healthCts;

    private const string Offline = "Sem conexão";
    private const string OfflineColor = "#e5573e";
    
    private const string Connected = "Conectado";
    private const string ConnectedColor = "#38A169";

    private void SetConnectedStatus(bool isConnected)
    {
        if (isConnected)
        {
            IsConnected = true;
            ConnectionStatus = Connected;
            ServerStatusColor = ConnectedColor;
        }
        else
        {
            IsConnected = false;
            ConnectionStatus = Offline;
            ServerStatusColor = OfflineColor;
        }
    }

    public MainWindowViewModel()
    {
        _inputCallback = InputCallbackImpl;
        
        var setupSettings = new SetupSettings();
        _melissaServerUrl = setupSettings.ReadServerAddress();
        
        SetConnectedStatus(false);
        StartHealthMonitor();

    }

    [RelayCommand]
    public async Task StartAudioCaptureAsync()
    {
        if (!IsConnected)
            return;

        try
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

            _ = Task.Run(async () =>
            {
                Console.WriteLine("[INFO] Iniciando task de envio/recepção de áudio...");
                var stream = _hubConnection.StreamAsync<byte[]>(
                    "AskMelissaAudio",
                    GetAudioStream(),
                    CancellationToken.None
                );

                var pipe = new Pipe();

                var readTask = Task.Run(async () =>
                {
                    await foreach (var replyBytes in stream)
                    {
                        await pipe.Writer.WriteAsync(replyBytes);

                        // Atualiza a propriedade com uma cópia da janela
                        Dispatcher.UIThread.Post(() => { IsPlayingAudio = true; });
                    }

                    await pipe.Writer.CompleteAsync();
                });

                var player = new Mpg123Wrapper();

                await player.PlayAudioFromStreamAsync(pipe.Reader.AsStream());
                IsPlayingAudio = false;

                await readTask;
            });

            PortAudio.Initialize();
            StartInputStream();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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

    [RelayCommand]
    private async Task OpenSettings()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsWindow = new SettingsWindow();

            var settingsViewModel = new SettingsViewModel(MelissaServerUrl, result => settingsWindow.Close(result));
            settingsWindow.DataContext = settingsViewModel;

            var result = await settingsWindow.ShowDialog<bool>(desktop.MainWindow!);
            if (result)
            {
                MelissaServerUrl = settingsViewModel.ServerAddress;
                Console.WriteLine($"[INFO] URL do servidor atualizada para: {MelissaServerUrl}");
            }
        }
    }
    private void StartHealthMonitor()
    {
        StopHealthMonitor();

        if (string.IsNullOrWhiteSpace(MelissaServerUrl))
        {
            Dispatcher.UIThread.Post(() => SetConnectedStatus(false));
            return;
        }

        _healthCts = new CancellationTokenSource();
        var token = _healthCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var urlBase = MelissaServerUrl.TrimEnd('/');
                    var url = $"{urlBase}/health";
                    var resp = await Http.GetAsync(url, token);

                    if (resp.IsSuccessStatusCode)
                    {
                        Dispatcher.UIThread.Post(() => SetConnectedStatus(true));
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => SetConnectedStatus(false));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignora cancelamento
                }
                catch (Exception)
                {
                    Dispatcher.UIThread.Post(() => SetConnectedStatus(false));
                }

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException)
                {
                    // Ignora cancelamento
                }
            }
        }, token);
    }

    private void StopHealthMonitor()
    {
        try
        {
            _healthCts?.Cancel();
            _healthCts?.Dispose();
        }
        catch { /* ignore */ }
        finally
        {
            _healthCts = null;
        }
    }

    partial void OnMelissaServerUrlChanged(string value)
    {
        StartHealthMonitor();
    }
}
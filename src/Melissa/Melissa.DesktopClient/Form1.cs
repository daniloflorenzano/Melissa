using Microsoft.AspNetCore.SignalR.Client;
using NAudio.Wave;
using System.Diagnostics;
using System.Media;
using System.Threading.Channels;

namespace Melissa.DesktopClient;

public partial class Form1 : Form
{
    bool stopRequested = false;

    public Form1()
    {
        InitializeComponent();
        _hubConnection = new HubConnectionBuilder()
           .WithUrl("http://localhost:5179/melissa")
           .WithAutomaticReconnect()
           .Build();

        _hubConnection.StartAsync().Wait();
    }

    private async void MicBtn_MouseDown(object sender, MouseEventArgs e)
    {
        stopRequested = false;
        await StartInteractionAsync();
    }

    private HubConnection _hubConnection;
    private BufferedWaveProvider _bufferedWaveProvider;
    private WaveInEvent _waveIn;


    private async Task StartInteractionAsync()
    {
        var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1)
        };

        var channel = Channel.CreateUnbounded<byte[]>();
        var responseStream = _hubConnection.StreamAsync<byte[]>("AskMelissaAudio", channel.Reader);

        waveIn.DataAvailable += (s, a) =>
        {
            var buffer = new byte[a.BytesRecorded];
            Array.Copy(a.Buffer, buffer, buffer.Length);
            channel.Writer.TryWrite(buffer); // envia chunk para o Hub
        };

        waveIn.RecordingStopped += (s, a) =>
        {
            channel.Writer.Complete(); // encerra envio para o servidor
        };

        waveIn.StartRecording();

        // Espera o usuário soltar o botão para parar
        await Task.Run(() =>
        {
            while (!stopRequested) Thread.Sleep(50);
        });

        waveIn.StopRecording();

        // Monta resposta do servidor (áudio gerado)
        using var mp3Stream = new MemoryStream();
        await foreach (var chunk in responseStream)
        {
            await mp3Stream.WriteAsync(chunk);
        }

        var wavBytes = await ConvertMp3StreamToWavAsync(mp3Stream);

        // Toca o WAV usando SoundPlayer
        using var ms = new MemoryStream(wavBytes);
        using var player = new SoundPlayer(ms);
        player.Play();
    }


    private void MicBtn_MouseUp(object sender, MouseEventArgs e)
    {
        stopRequested = true;
    }

    public static async Task<byte[]> ConvertMp3StreamToWavAsync(Stream mp3Stream, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.GetTempPath();
        var mp3Path = Path.Combine(tempDir, $"{Guid.NewGuid()}.mp3");
        var wavPath = Path.Combine(tempDir, $"{Guid.NewGuid()}.wav");

        // Salva stream em um arquivo temporário
        await using (var fileStream = File.Create(mp3Path))
        {
            mp3Stream.Position = 0;
            await mp3Stream.CopyToAsync(fileStream, cancellationToken);
        }

        // Executa ffmpeg
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -i \"{mp3Path}\" -ar 16000 -ac 1 -sample_fmt s16 \"{wavPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null) throw new Exception("Não foi possível iniciar o ffmpeg.");

        await process.WaitForExitAsync(cancellationToken);

        if (!File.Exists(wavPath))
            throw new Exception("Conversão para WAV falhou. Verifique se o ffmpeg está no PATH.");

        var wavBytes = await File.ReadAllBytesAsync(wavPath, cancellationToken);

        // Limpeza
        File.Delete(mp3Path);
        File.Delete(wavPath);

        return wavBytes;
    }
}
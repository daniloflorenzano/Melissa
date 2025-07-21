using System.Diagnostics;
using Serilog;

namespace MeetRecorder.Cli;

public sealed class AudioRecorder : IDisposable
{
    private Process? _ffmpegProcess;
    private readonly string _outputDirectory;
    private string? _currentOutputPath;
    private Task _completedTask = Task.CompletedTask;

    public AudioRecorder(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(_outputDirectory);
    }

    public Task StartRecording()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _currentOutputPath = Path.Combine(_outputDirectory, $"meeting_audio_{timestamp}.wav");

        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-f alsa -i default -ac 1 -ar 16000 -y \"{_currentOutputPath}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            _ffmpegProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            // Event handler para logar erro e saída, para monitorar o processo
            _ffmpegProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Log.Debug("[ffmpeg stdout] {Line}", e.Data);
            };

            _ffmpegProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Log.Error("[ffmpeg stderr] {Line}", e.Data);
            };

            _ffmpegProcess.Exited += (s, e) =>
            {
                Log.Warning("ffmpeg process exited with code {Code}", _ffmpegProcess.ExitCode);
            };

            _ffmpegProcess.Start();

            // Começa a leitura assíncrona para evitar bloqueio por buffers cheios
            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();

            Log.Information("Gravação iniciada em: {OutputPath}", _currentOutputPath);
            Console.WriteLine($"Gravação iniciada em: {_currentOutputPath}");
            return _completedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao iniciar gravação");
            Console.WriteLine("Erro ao iniciar gravação: " + ex.Message);
            return _completedTask;
        }
    }


    public async Task<string?> StopRecording()
    {
        if (_ffmpegProcess == null || _ffmpegProcess.HasExited)
        {
            Log.Warning("Nenhuma gravação em andamento.");
            Console.WriteLine("Nenhuma gravação em andamento.");
            return null;
        }

        try
        {
            _ffmpegProcess.Kill(true);
            await _ffmpegProcess.WaitForExitAsync();

            Log.Information("Gravação finalizada: {CurrentOutputPath}", _currentOutputPath);
            Console.WriteLine($"Gravação finalizada: {_currentOutputPath}");

            var finalPath = _currentOutputPath;
            _currentOutputPath = null;
            _ffmpegProcess.Dispose();
            _ffmpegProcess = null;

            return finalPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao parar gravação");
            Console.WriteLine("Erro ao parar gravação: " + ex.Message);
            return null;
        }
    }

    public void Dispose()
    {
        if (_ffmpegProcess is { HasExited: false })
        {
            try
            {
                Log.Information("Encerrando processo: {ProcessId} - {ProcessName}", _ffmpegProcess.Id,
                    _ffmpegProcess.ProcessName);
                _ffmpegProcess.Kill(true);
                _ffmpegProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao encerrar o processo ffmpeg");
            }
        }

        _ffmpegProcess?.Dispose();
    }
}
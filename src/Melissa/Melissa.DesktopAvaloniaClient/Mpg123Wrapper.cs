using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Melissa.DesktopAvaloniaClient;

public class Mpg123Wrapper
{
    public bool IsPlaying { get; private set; }
    public event EventHandler PlaybackFinished;

    public async Task PlayAudioFromStreamAsync(Stream audioStream)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mpg123",
                    Arguments = "-",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // espera um pouco se nao perde o inicio do audio
            await Task.Delay(100);

            IsPlaying = true;

            await audioStream.CopyToAsync(process.StandardInput.BaseStream);
            process.StandardInput.Close();

            await process.WaitForExitAsync();

            IsPlaying = false;
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            // Log ou tratamento adicional
            await Console.Error.WriteLineAsync($"Erro ao reproduzir áudio: {ex.Message}");
            throw;
        }
    }
}
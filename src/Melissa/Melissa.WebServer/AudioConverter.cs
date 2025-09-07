using System.Diagnostics;

namespace Melissa.WebServer;

public static class AudioConverter
{
    public static async Task<byte[]> ConvertMp3StreamToWavAsync(Stream mp3Stream, CancellationToken cancellationToken = default)
    {
        var tempDir = Path.GetTempPath();
        var mp3Path = Path.Combine(tempDir, $"{Guid.NewGuid()}.mp3");
        var wavPath = Path.Combine(tempDir, $"{Guid.NewGuid()}.wav");

        // Salva stream em um arquivo tempor�rio
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
        if (process == null) throw new Exception("Nao foi possivel iniciar o ffmpeg.");

        await process.WaitForExitAsync(cancellationToken);

        if (!File.Exists(wavPath))
            throw new Exception("Conversao para WAV falhou. Verifique se o ffmpeg esta no PATH.");

        var wavBytes = await File.ReadAllBytesAsync(wavPath, cancellationToken);

        // Limpeza
        File.Delete(mp3Path);
        File.Delete(wavPath);

        return wavBytes;
    }
}
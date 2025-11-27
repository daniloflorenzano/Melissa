namespace Melissa.WebServer;

public static class AudioDecoder
{
    /// <summary>
    /// Decodifica AAC/M4A para PCM 16-bit
    /// </summary>
    public static async Task<byte[]> DecodeAACToPCM(byte[] aacData)
    {
        // Opção 1: Usar FFmpeg (mais confiável)
        return await DecodeUsingFFmpeg(aacData);
        
        // Opção 2: Se usar NAudio
        // return DecodeUsingNAudio(aacData);
    }

    private static async Task<byte[]> DecodeUsingFFmpeg(byte[] aacData)
    {
        var tempAacFile = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.m4a");
        var tempPcmFile = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.pcm");

        try
        {
            // Salvar AAC temporariamente
            await File.WriteAllBytesAsync(tempAacFile, aacData);

            // Executar FFmpeg para decodificar
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{tempAacFile}\" -acodec pcm_s16le -ar 16000 -ac 1 -f s16le \"{tempPcmFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException($"FFmpeg failed: {error}");
                }
            }

            // Ler PCM decodificado
            return await File.ReadAllBytesAsync(tempPcmFile);
        }
        finally
        {
            // Limpar arquivos temporários
            if (File.Exists(tempAacFile)) File.Delete(tempAacFile);
            if (File.Exists(tempPcmFile)) File.Delete(tempPcmFile);
        }
    }
}
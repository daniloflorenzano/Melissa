using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Melissa.DesktopAvaloniaClient;

public class Mpg123Wrapper
{
    public static async Task PlayAudioFromStreamAsync(Stream audioStream)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "mpg123",
                Arguments = "-", // lê do stdin
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Aguarda o processo estar pronto para receber dados
        // sem isso o primeiro segundo do audio se perde
        await Task.Delay(100); 

        // Copia os bytes do MP3 para o stdin do mpg123
        await audioStream.CopyToAsync(process.StandardInput.BaseStream);
        process.StandardInput.Close();

        await process.WaitForExitAsync();
    }
}
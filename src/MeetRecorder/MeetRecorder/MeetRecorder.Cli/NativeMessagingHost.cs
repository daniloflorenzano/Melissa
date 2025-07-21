using System.Diagnostics;
using System.Text.Json;
using System.Text;
using Serilog;

namespace MeetRecorder.Cli;

public sealed class NativeMessagingHost : IDisposable
{
    private readonly AudioRecorder _recorder;

    public NativeMessagingHost()
    {
        var outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "MeetRecordings");
        _recorder = new AudioRecorder(outputDir);
    }

    public async Task RunAsync()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var count = 0;
        var stdin = Console.OpenStandardInput();

        while (true)
        {
            count++;
            Log.Information("Vezes no loop: {Count}", count);

            var lengthBytes = new byte[4];
            int readLen = stdin.Read(lengthBytes, 0, 4);
            if (readLen == 0)
            {
                Log.Warning("Encerrando loop — pipe fechado pelo navegador");
                break;
            }

            int messageLength = BitConverter.ToInt32(lengthBytes, 0);
            var buffer = new byte[messageLength];
            int readBytes = 0;
            while (readBytes < messageLength)
            {
                int n = stdin.Read(buffer, readBytes, messageLength - readBytes);
                if (n == 0)
                {
                    Log.Warning("Encerrando loop — leitura incompleta");
                    break;
                }

                readBytes += n;
            }

            string json = Encoding.UTF8.GetString(buffer);
            var message = JsonDocument.Parse(json).RootElement;

            Log.Information("Recebido: {Json}", json);
            Console.Error.WriteLine($"[Host] Recebido: {json}");

            string status = "unknown";
            string? filePath = null;

            if (message.TryGetProperty("command", out var commandElement))
            {
                var command = commandElement.GetString();

                switch (command)
                {
                    case "startRecording":
                        try
                        {
                            Log.Information("Iniciando gravação");
                            Console.Error.WriteLine("[Host] Iniciando gravação");
                            _recorder.StartRecording();
                            status = "recording_started";
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Erro ao iniciar gravação");
                            status = "error";
                        }

                        break;


                    case "stopRecording":
                        Log.Information("Parando gravação");
                        Console.Error.WriteLine("[Host] Parando gravação");
                        filePath = await _recorder.StopRecording();
                        status = filePath != null ? "recording_stopped" : "error";
                        break;
                }
            }

            var response = JsonSerializer.Serialize(new
            {
                status,
                path = filePath
            });

            WriteMessage(response);
            while (true)
            {
                await Task.Delay(1000);
            }
        }
    }

    private void WriteMessage(string json)
    {
        Log.Information("Enviando resposta: {Json}", json);
        var bytes = Encoding.UTF8.GetBytes(json);
        var length = BitConverter.GetBytes(bytes.Length);
        Console.OpenStandardOutput().Write(length, 0, 4);
        Console.OpenStandardOutput().Write(bytes, 0, bytes.Length);
        Console.Out.Flush();
    }

    public void Dispose()
    {
        _recorder.Dispose();
    }
}
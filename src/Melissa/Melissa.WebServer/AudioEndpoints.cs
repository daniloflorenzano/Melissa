using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using edge_tts_net;
using Melissa.Core.Assistants;
using Serilog;
using Whisper.net;
using Whisper.net.Ggml;
using MelissaAssistant = Melissa.Core.Assistants.Melissa;

public static class AudioEndpoints
{
    public static async Task<IResult> AskMelissaAudio(HttpRequest request, MelissaAssistant melissa)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        using var ms = new MemoryStream();
        try
        {
            await request.Body.CopyToAsync(ms);
        }
        catch (Exception ex)
        {
            Log.Warning("Requisição cancelada antes do tempo: {0}", ex.Message);
            return Results.StatusCode(408); // Request Timeout
        }


        // Carrega ou baixa o modelo Whisper se necessário
        var ggmlType = GgmlType.Medium;
        var modelFileName = "ggml-medium.bin";
        if (!File.Exists(modelFileName))
        {
            await DownloadModel(modelFileName, ggmlType);
        }

        // Inicializa Whisper
        using var whisperFactory = WhisperFactory.FromPath(modelFileName);
        await using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("pt")
            .Build();

        var pcmBytes = ms.ToArray();
        var wavBytes = GenerateWav(pcmBytes);

        using var wavStream = new MemoryStream(wavBytes);
        wavStream.Seek(0, SeekOrigin.Begin);

        var transcriptionBuilder = new StringBuilder();
        await foreach (var result in processor.ProcessAsync(wavStream, cancellationToken))
        {
            transcriptionBuilder.Append(result.Text);
        }

        var userMessage = transcriptionBuilder.ToString();
        Log.Information("Usuário: {0}", userMessage);

        if (userMessage.Contains("[SILENCIO]", StringComparison.CurrentCultureIgnoreCase))
            return Results.Ok("Silêncio detectado, não há resposta da assistente.");

        var question = new Question(userMessage, "AudioHub", DateTimeOffset.Now);
        var replyBuilder = new StringBuilder();

        var (isAvailable, statusMessage) = await melissa.CanUse();
        if (isAvailable)
        {
            await foreach (var t in SafeAskMelissa(melissa, question, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                replyBuilder.Append(t);
            }
        }
        else
        {
            replyBuilder.Append(statusMessage);
        }

        // Síntese com EdgeTTS
        var edgeTts = new EdgeTTSNet();
        var voices = await edgeTts.GetVoices();
        var ptVoice = voices.FirstOrDefault(v => v.ShortName == "pt-BR-FranciscaNeural") ?? voices.First();

        var ttsOptions = new TTSOption(ptVoice.Name, "+0Hz", "+25%", "+0%");
        edgeTts = new EdgeTTSNet(ttsOptions);

        var replyText = replyBuilder.ToString();
        Log.Information("Assistente: {0}", replyText);

        var tempMp3File = Path.Combine(Path.GetTempPath(), "reply.mp3");
        await edgeTts.Save(replyText, tempMp3File, cancellationToken);

        var replyBytes = await File.ReadAllBytesAsync(tempMp3File, cancellationToken);
        File.Delete(tempMp3File);

        return Results.File(replyBytes, contentType: "audio/mpeg");
    }

    // WAV header generation
    private static byte[] GenerateWav(byte[] pcm)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        int sampleRate = 16000;
        short bitsPerSample = 16;
        short channels = 1;

        // RIFF header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + pcm.Length);
        writer.Write("WAVE".ToCharArray());

        // fmt subchunk
        writer.Write("fmt ".ToCharArray());
        writer.Write(16); // Subchunk1Size
        writer.Write((short)1); // AudioFormat PCM
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8); // ByteRate
        writer.Write((short)(channels * bitsPerSample / 8)); // BlockAlign
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write("data".ToCharArray());
        writer.Write(pcm.Length);
        writer.Write(pcm);

        return output.ToArray();
    }

    private static async IAsyncEnumerable<string> SafeAskMelissa(MelissaAssistant melissa, Question question,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<string>();

        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in melissa.Ask(question, cancellationToken))
                {
                    await channel.Writer.WriteAsync(item, cancellationToken);
                }
            }
            catch (Exception e)
            {
                await channel.Writer.WriteAsync(melissa.UnavailabilityMessage, cancellationToken);
                Log.Error(e, "Resposta da assistente interrompida por uma exceção");
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (channel.Reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }
    
    static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
        await using var fileWriter = File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }
}

using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using edge_tts_net;
using Melissa.Core.Assistants;
using Melissa.Core.ExternalData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Whisper.net;
using Whisper.net.Ggml;
using MelissaAssistant = Melissa.Core.Assistants.Melissa;

namespace Melissa.WebServer;

public class MelissaHub : Hub
{
    public async IAsyncEnumerable<string> AskMelissaText(string message, [FromServices] MelissaAssistant melissa,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (isAvailable, statusMessage) = await melissa.CanUse();
        if (!isAvailable)
            yield return statusMessage;
        
        var question = new Question(message, "TextHub", DateTimeOffset.Now);
        await foreach (var t in SafeAskMelissa(melissa, question, cancellationToken))
        {
            yield return t;
        }
    }

    public async IAsyncEnumerable<byte[]> AskMelissaAudio(IAsyncEnumerable<byte[]> audioStream, [FromServices] MelissaAssistant melissa,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();

        await foreach (var chunk in audioStream.WithCancellation(cancellationToken))
        {
            await ms.WriteAsync(chunk, cancellationToken);
        }

        var ggmlType = GgmlType.Medium;
        var modelFileName = "ggml-medium.bin";

        if (!File.Exists(modelFileName))
        {
            await DownloadModel(modelFileName, ggmlType);
        }
        
        using var whisperFactory = WhisperFactory.FromPath("ggml-medium.bin");
        await using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("pt")           
            .Build();

        var pcmBytes = ms.ToArray();
        var wavBytes = GenerateWav(pcmBytes);

        var wavStream = new MemoryStream(wavBytes);
        wavStream.Seek(0, SeekOrigin.Begin);

        var msgBuilder = new StringBuilder();
        await foreach (var result in processor.ProcessAsync(wavStream, cancellationToken))
        {
            msgBuilder.Append(result.Text);
        }
        
        var message = msgBuilder.ToString();
        Log.Information("Usuário: {0}", message);
        
        var question = new Question(message, "AudioHub", DateTimeOffset.Now);
        string melissaReply;
        var (isAvailable, statusMessage) = await melissa.CanUse();
        
        if (isAvailable)
            melissaReply = await MelissaHub.SafeAskMelissaWithErrorHandlingAndRetry(melissa, question, cancellationToken);
        else
            melissaReply = statusMessage;

        
        var edgeTts = new EdgeTTSNet();
    
        var voices = await edgeTts.GetVoices();
        var cnVoice = voices.FirstOrDefault(v => v.ShortName == "pt-BR-FranciscaNeural");
        var options = new TTSOption
        (
            voice: cnVoice.Name,
            pitch: "+0Hz",
            rate: "+25%",
            volume: "+0%"
        );
        
        Log.Information("Assistente: {0}", melissaReply);
        
        var tempFilePath = Path.Combine(Path.GetTempPath(), "temp.mp3");
        var fs = new FileStream(tempFilePath, FileMode.Create);

        edgeTts = new EdgeTTSNet(options);
        await edgeTts.TTS(melissaReply, (metaObj) =>
        {
            if (metaObj.Type == TTSMetadataType.Audio)
            {
                fs.Write(metaObj.Data);
            }
        }, cancellationToken);

        var fsAsWav = await AudioConverter.ConvertMp3StreamToWavAsync(fs, cancellationToken);
        
        await fs.FlushAsync(cancellationToken);
        await fs.DisposeAsync();
        
        File.Delete(tempFilePath);

        yield return fsAsWav;
    }
    
    private static byte[] GenerateWav(byte[] pcmData, int sampleRate = 16000, short bitsPerSample = 16, short channels = 1)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int subChunk2Size = pcmData.Length;
        int chunkSize = 36 + subChunk2Size;

        // RIFF header
        writer.Write("RIFF"u8.ToArray());
        writer.Write(chunkSize);
        writer.Write("WAVE"u8.ToArray());

        // fmt subchunk
        writer.Write("fmt "u8.ToArray());
        writer.Write(16); // Subchunk1Size for PCM
        writer.Write((short)1); // AudioFormat = PCM
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write("data"u8.ToArray());
        writer.Write(subChunk2Size);
        writer.Write(pcmData);

        writer.Flush();
        return ms.ToArray();
    }
    
    static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        await using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
        await using var fileWriter = File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }

    public static async IAsyncEnumerable<string> SafeAskMelissa(MelissaAssistant melissa, Question question,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<string>();
        var historyData = new DbConversationHistory();
        historyData.Pergunta = question.Text;
        
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var item in melissa.Ask(question, cancellationToken))
                {
                    historyData.Resposta += item;
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
                await DatabaseFeeder.FeedHistoryData(historyData);
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
    
    public static async Task<string> SafeAskMelissaWithErrorHandlingAndRetry(MelissaAssistant melissa, Question question,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tryCount = 0;

            while (tryCount < 3)
            {
                tryCount++;
                
                var replyBuilder = new StringBuilder();
                await foreach (var item in SafeAskMelissa(melissa, question, cancellationToken))
                {
                    replyBuilder.Append(item);
                }
                var reply = replyBuilder.ToString();

                if (string.IsNullOrWhiteSpace(reply))
                {
                    Log.Warning("Resposta da assistente está vazia, reiniciando o chat e tentando novamente...");
                    melissa.ResetChat();
                }

                if (reply.StartsWith('{'))
                {
                    Log.Warning("Resposta da assistente provavelmente é um JSON, repetindo a pergunta...: {0}", reply);
                    continue;
                }
                
                return reply;
            }

            return melissa.UnavailabilityMessage;
        }
        catch (Exception e)
        {
            Log.Error(e, "Erro ao perguntar a assistente Melissa");
            return melissa.UnavailabilityMessage;
        }
    }
}
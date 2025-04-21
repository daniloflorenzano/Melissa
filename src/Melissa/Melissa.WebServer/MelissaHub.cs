using System.Runtime.CompilerServices;
using System.Text;
using edge_tts_net;
using Melissa.Core.Assistants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Whisper.net;
using Whisper.net.Ggml;
using MelissaAssistant = Melissa.Core.Assistants.Melissa;

namespace Melissa.WebServer;

public class MelissaHub : Hub
{
    public async IAsyncEnumerable<string> AskMelissaText(string message, [FromServices] MelissaAssistant melissa,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var question = new Question(message, "TextHub", DateTimeOffset.Now);
        await foreach (var t in melissa.Ask(question, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        
        using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");
        await using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("pt")           
            .Build();

        var pcmBytes = ms.ToArray();
        var wavBytes = GenerateWav(pcmBytes);

        File.WriteAllBytes("teste.wav", wavBytes);

        var wavStream = new MemoryStream(wavBytes);
        wavStream.Seek(0, SeekOrigin.Begin);

        var msgBuilder = new StringBuilder();
        await foreach (var result in processor.ProcessAsync(wavStream, cancellationToken))
        {
            msgBuilder.Append(result.Text);
        }
        
        var question = new Question(msgBuilder.ToString(), "AudioHub", DateTimeOffset.Now);
        var replyBuilder = new StringBuilder();
        
        await foreach (var t in melissa.Ask(question, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            replyBuilder.Append(t);
        }
        
        
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

        var tempMp3File = Path.GetTempPath() + "temp.mp3";
        edgeTts = new EdgeTTSNet(options);
        await edgeTts.Save(replyBuilder.ToString(), tempMp3File, cancellationToken);
        
        var replyBytes = await File.ReadAllBytesAsync(tempMp3File, cancellationToken);
        File.Delete(tempMp3File);
        
        yield return replyBytes;
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
}
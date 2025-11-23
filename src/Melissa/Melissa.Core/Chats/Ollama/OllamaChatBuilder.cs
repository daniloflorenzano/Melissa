using System.Text;
using Melissa.Core.Utils;
using OllamaSharp;
using OllamaSharp.Models;
using Serilog;

namespace Melissa.Core.Chats.Ollama;

public class OllamaChatBuilder : IChatBuilder
{
    public ModelName ModelName { get; set; }
    public ModelName? ModelFrom { get; set; }
    public Dictionary<string, object> Parameters { get; } = new();
    public string SystemMessage { get; set; } = string.Empty;
    public List<object> Tools { get; } = [];
    
    public IChatBuilder AddTool(object tool)
    {
        Tools.Add(tool);
        return this;
    }

    public async Task<IChat> Build()
    {
        try
        {
            var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_URL") 
                          ?? Environment.GetEnvironmentVariable("OllamaUrl") 
                          ?? "http://localhost:11434";
            var uri = new Uri(baseUrl);
            var ollama = new OllamaApiClient(uri);

            var modelName = $"{EnumHelper.GetEnumDescription(ModelName)}:latest";
            ollama.SelectedModel = modelName;

            var availableModels = await ollama.ListLocalModelsAsync();
            var isSelectedModelAvailable = availableModels.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase));

            if (!isSelectedModelAvailable)
            {
                Log.Warning("Modelo {modelName} não disponível localmente.", modelName);
                
                if (ModelFrom is null)
                    await foreach (var status in ollama.PullModelAsync(modelName))
                        Console.WriteLine($"{status?.Percent}% {status?.Status}");
                else
                    await CreateModel(ollama);
            }

            var chat = new Chat(ollama);
            return new OllamaChatWrapper(chat, Tools);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e, "Erro ao iniciar chat. Provavelmente o servidor Ollama está offline ou em uma porta diferente da esperada.");
            throw;
        }
        catch (Exception e)
        {
            Log.Error(e, "Erro ao iniciar chat.");
            throw;
        }
    }

    private async Task CreateModel(OllamaApiClient client)
    {
        Log.Information("Criando modelo {modelName} a partir de {modelFrom}", ModelName, ModelFrom);
        
        var availableModels = await client.ListLocalModelsAsync();
        var isModelFromAvailable = availableModels
            .Any(m => m.Name.Equals(EnumHelper.GetEnumDescription(ModelFrom!), StringComparison.OrdinalIgnoreCase));

        if (!isModelFromAvailable) 
            Log.Warning("Modelo base {modelFrom} não disponível. Iniciando download...", ModelFrom);
        else 
            Log.Information("Modelo base {modelFrom} disponível. Pulando download e eniciando criação do modelo {modelName}...", ModelFrom, ModelName);
        
        var builder = new StringBuilder();

        var modelStream = client.CreateModelAsync(new CreateModelRequest
        {
            Model = EnumHelper.GetEnumDescription(ModelName),
            From = EnumHelper.GetEnumDescription(ModelFrom!),
            System = SystemMessage,
            Parameters = Parameters
        }, CancellationToken.None);
        
        await foreach (var status in modelStream)
            if (status != null) builder.Append(status.Status);
        
        Log.Information("Modelo criado: {modelName}. Status: {status}", ModelName, builder.ToString());
    }
}
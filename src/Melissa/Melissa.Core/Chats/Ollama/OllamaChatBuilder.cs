using Melissa.Core.Utils;
using OllamaSharp;
using Serilog;

namespace Melissa.Core.Chats.Ollama;

public class OllamaChatBuilder : IChatBuilder
{
    public ModelName ModelName { get; set; }
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
            var uri = new Uri("http://localhost:11434");
            var ollama = new OllamaApiClient(uri);

            var modelName = EnumHelper.GetEnumDescription(ModelName);
            ollama.SelectedModel = modelName;

            var availableModels = await ollama.ListLocalModelsAsync();
            var isSelectedModelAvailable = availableModels.Any(m => m.Name.Equals(m.Name, StringComparison.OrdinalIgnoreCase));

            if (!isSelectedModelAvailable)
            {
                await foreach (var status in ollama.PullModelAsync(modelName))
                    Console.WriteLine($"{status?.Percent}% {status?.Status}");
            }

            var chat = new Chat(ollama, SystemMessage);
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
}
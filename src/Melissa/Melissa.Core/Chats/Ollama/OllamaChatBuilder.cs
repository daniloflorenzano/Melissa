using Melissa.Core.Utils;
using OllamaSharp;

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
        var uri = new Uri("http://localhost:11434");
        var ollama = new OllamaApiClient(uri);
        
        var modelName = EnumHelper.GetEnumDescription(ModelName);
        ollama.SelectedModel = modelName;

        await foreach (var status in ollama.PullModelAsync(modelName))
            Console.WriteLine($"{status.Percent}% {status.Status}");

        var chat = new Chat(ollama, SystemMessage);
        
        return new OllamaChatWrapper(chat, Tools);
    }
}
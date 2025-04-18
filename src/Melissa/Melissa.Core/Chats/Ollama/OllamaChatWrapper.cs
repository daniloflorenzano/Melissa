using OllamaSharp;

namespace Melissa.Core.Chats.Ollama;

public class OllamaChatWrapper(Chat ollamaChat, List<object> tools) : IChat
{
    public IAsyncEnumerable<string> SendAsync(string message, IEnumerable<object> tools, CancellationToken cancellationToken = default)
    {
        return ollamaChat.SendAsync(message, tools: tools, cancellationToken: cancellationToken);
    }

    public IAsyncEnumerable<string> SendAsync(string message, CancellationToken cancellationToken = default)
    {
        return tools.Count == 0 
            ? ollamaChat.SendAsync(message, cancellationToken) 
            : SendAsync(message, tools, cancellationToken);
    }

    public async Task<bool> IsChatReady()
    {
        return await ollamaChat.Client.IsRunningAsync();
    }

    public Task ChangeModel(ModelName modelName)
    {
        throw new NotImplementedException();
    }
}
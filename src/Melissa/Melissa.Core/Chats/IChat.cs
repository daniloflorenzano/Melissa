namespace Melissa.Core.Chats;

public interface IChat
{
    public IAsyncEnumerable<string> SendAsync(string message,
        CancellationToken cancellationToken = default);

    public Task<bool> IsChatReady();
    Task ChangeModel(ModelName modelName);
}
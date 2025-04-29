using Melissa.Core.Chats;
using Serilog;

namespace Melissa.Core.Assistants;

public abstract class Assistant
{
    public abstract string Name { get; }
    
    protected IChat Chat = null!;

    protected Assistant(IChatBuilder chatBuilder)
    {
    }

    public async Task<bool> CanUse()
    {
        try
        {
            return await Chat.IsChatReady();
        }
        catch (Exception e)
        {
            Log.Error(e, "{assistantName} não está disponível.", Name);
            return false;
        }
    }
    
    public virtual IAsyncEnumerable<string> Ask(Question question, CancellationToken cancellationToken = default)
    {
        if (Chat is null)
            throw new InvalidOperationException("O chat não foi carregado corretamente.");
        
        return Chat.SendAsync(question.Text, cancellationToken);
    }
}
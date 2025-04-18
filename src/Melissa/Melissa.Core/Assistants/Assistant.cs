using Melissa.Core.Chats;

namespace Melissa.Core.Assistants;

public abstract class Assistant
{
    protected IChat Chat = null!;

    protected Assistant(IChatBuilder chatBuilder)
    {
    }

    public async Task<bool> CanUse()
    {
        return await Chat.IsChatReady();
    }
    
    public virtual IAsyncEnumerable<string> Ask(Question question)
    {
        if (Chat is null)
            throw new InvalidOperationException("O chat não foi carregado corretamente.");
        
        return Chat.SendAsync(question.Text);
    }
}
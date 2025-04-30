using Melissa.Core.Chats;
using Serilog;

namespace Melissa.Core.Assistants;

public abstract class Assistant
{
    public abstract string Name { get; }
    
    protected IChat Chat = null!;
    public abstract string UnavailabilityMessage { get; }

    protected Assistant(IChatBuilder chatBuilder)
    {
    }

    public async Task<(bool isAvailable, string statusMessage)> CanUse()
    {
        try
        {
            var canUse = await Chat.IsChatReady();
            return (canUse, string.Empty);
        }
        catch (Exception e)
        {
            Log.Error(e, "{assistantName} não está disponível.", Name);
            return (false, UnavailabilityMessage);
        }
    }
    
    public virtual IAsyncEnumerable<string> Ask(Question question, CancellationToken cancellationToken = default)
    {
        if (Chat is null)
            throw new InvalidOperationException("O chat não foi carregado corretamente.");
        
        return Chat.SendAsync(question.Text, cancellationToken);
    }
}
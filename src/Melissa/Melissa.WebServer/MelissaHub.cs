using System.Runtime.CompilerServices;
using Melissa.Core.Assistants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
}
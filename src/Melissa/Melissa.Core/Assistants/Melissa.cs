using Melissa.Core.AiTools.Holidays;
using Melissa.Core.AiTools.Time;
using Melissa.Core.AiTools.Weather;
using Melissa.Core.Chats;

namespace Melissa.Core.Assistants;

public class Melissa : Assistant
{
    public sealed override string Name => nameof(Melissa);
    public sealed override string UnavailabilityMessage => "Desculpe, parece que não consigo te responder no momento. Por favor, confira se o Ollama está em execução.";
    
    public Melissa(IChatBuilder chatBuilder) : base(chatBuilder)
    {
        chatBuilder
            .WithModelName(ModelName.Melissa)
            .WithTool(new GetWeatherByLocationTool())
            .WithTool(new GetBrazilianHolidaysTool())
            .WithTool(new GetHolidayDateByNameTool())
            .WithTool(new GetCurrentDateTimeTool());
        Chat = chatBuilder.Build().Result;
    }

    public bool HasInternetAccess { get; private set; }
    public bool IsUsingCryptography { get; private set; }

    /// <summary>
    /// Liga/desliga o acesso à internet.
    /// Caso desligada, a assistente não poderá acessar APIs externas.
    /// </summary>
    public void ToggleInternetAccess() => HasInternetAccess = !HasInternetAccess;

    /// <summary>
    /// Liga/desliga o uso de criptografia das mensagens.
    /// Se ligada, espera-se que o usuário também envie mensagens criptografadas.
    /// Funciona como uma camada extra de segurança.
    /// </summary>
    public void ToggleCryptography() => IsUsingCryptography = !IsUsingCryptography;

    public async Task ChangeModel(ModelName modelName)
    {
        await Chat.ChangeModel(modelName);
    }

    public override IAsyncEnumerable<string> Ask(Question question, CancellationToken cancellationToken = default)
    {
        if (IsUsingCryptography)
        {
            // TODO: Implementar criptografia

            // decrypta a mensagem
            var decrypted = question.Text;
            var copy = question with { Text = decrypted };

            // encrypta a resposta
            return base.Ask(copy, cancellationToken);
        }

        return base.Ask(question, cancellationToken);
    }
}
using Melissa.Core.AiTools.Holidays;
using Melissa.Core.AiTools.Time;
using Melissa.Core.AiTools.Weather;
using Melissa.Core.Chats;

namespace Melissa.Core.Assistants;

public class Melissa : Assistant
{
    public Melissa(IChatBuilder chatBuilder) : base(chatBuilder)
    {
        var currentDate = DateTime.Now.Date;
        chatBuilder
            .WithModelName(ModelName.Llama32_3B)
            .WithAssistantName("Melissa")
            .WithPurposeDescription("""
                                    Ser uma assistente pessoal inteligente chamada Melissa, capaz de responder perguntas gerais e usar ferramentas externas quando necessário.
                                    Use ferramentas quando a pergunta envolver informações específicas como datas de feriados no Brasil.
                                    """)
            .WithAdicionalDescription($"Responda de forma breve, como se estivesse falando oralmente, usando frases curtas e diretas. O dia atual é {currentDate}")
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
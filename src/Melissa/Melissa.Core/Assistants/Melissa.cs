using Melissa.Core.AiTools.Holidays;
using Melissa.Core.AiTools.Localization;
using Melissa.Core.AiTools.Time;
using Melissa.Core.AiTools.Weather;
using Melissa.Core.Chats;
using Melissa.WebServer;

namespace Melissa.Core.Assistants;

public class Melissa : Assistant
{
    public sealed override string Name => nameof(Melissa);
    public sealed override string UnavailabilityMessage => "Desculpe, parece que não consigo te responder no momento. Por favor, confira se o Ollama está em execução.";
    
    public Melissa(IChatBuilder chatBuilder) : base(chatBuilder)
    {
        chatBuilder
            .WithModelName(ModelName.Llama32_3B)
            .WithAssistantName(Name)
            .WithPurposeDescription($"Ser uma assistente pessoal inteligente chamada {Name}, capaz de responder perguntas gerais e usar ferramentas específicas quando necessário.")
            .WithAdicionalDescription("Sempre responda seu propósito quando for perguntado ou solicitado que se apresente.")
            .WithAdicionalDescription("Responda de forma breve, como se estivesse falando oralmente, usando frases curtas, diretas e sempre em português do Brasil.")
            .WithAdicionalDescription("NÃO utilize qualquer formatação em suas respostas.")
            .WithAdicionalDescription("NÃO informe o nome de suas ferramentas nas respostas.")
            .WithAdicionalDescription("Se o usuário pedir informações sobre feriados, utilize sua ferramenta GetBrazilianHolidaysTool.")
            .WithAdicionalDescription("Se o usuário perguntar sobre uma data específica, utilize sua ferramenta GetHolidayDateByNameTool.")
            .WithAdicionalDescription("Se o usuário precisar saber sobre o dia ou a hora atual, utilize sua ferramenta GetCurrentDateTimeTool.")
            .WithAdicionalDescription("Se o usuário precisar do histórico de conversa de um determinado período (ultima hora, ultimo dia ou ultimo mês), utilize sua ferramenta SendEmailHistoryDataByPeriodTool. Retorne que o email foi enviado com sucesso em casos de sucesso.")
            .WithAdicionalDescription("Se o usuário precisar saber sobre o clima ou temperatura de um determinado local, utilize sua ferramenta GetCurrentTemperatureByLocationTool. Nunca deixe de informar os Graus Celsius atual. Seja curta e direta na sua resposta.")
            .WithAdicionalDescription("Se o usuário pedir informações sobre uma cidade, utilize sua ferramenta GetCityInfoTool para obter informações sobre a cidade, incluindo classificação, população, estado, país, hospitais, escolas, atrações turísticas e se possui aeroporto ou estação de trem.")
            .WithAdicionalDescription("Sempre utilize sua ferramenta GetCurrentDateTimeTool internamente para melhorar suas respostas.")
            .WithTool(new GetCurrentTemperatureByLocationTool())
            .WithTool(new GetBrazilianHolidaysTool())
            .WithTool(new GetHolidayDateByNameTool())
            .WithTool(new GetCityInfoTool())
            .WithTool(new GetCurrentDateTimeTool())
            .WithTool(new SendEmailConversationHistoryByPeriodTool());
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
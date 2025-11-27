using Melissa.Core.AiTools.Holidays;
using Melissa.Core.AiTools.Localization;
using Melissa.Core.AiTools.Time;
using Melissa.Core.AiTools.Weather;
using Melissa.Core.AiTools.TaskList;
using Melissa.Core.Chats;
using Melissa.WebServer;

namespace Melissa.Core.Assistants;

public class Melissa : Assistant
{
    public sealed override string Name => nameof(Melissa);
    public sealed override string UnavailabilityMessage => "Desculpe, parece que não consigo te responder no momento. Por favor, confira se o Ollama está em execução.";
    
    private readonly IChatBuilder _chatBuilder;

    private const string SystemMessage = """
                                         O seu objetivo é ser uma assistente pessoal inteligente chamada Melissa, capaz de responder perguntas gerais e usar ferramentas específicas quando necessário.
                                         Além disso, sempre responda seu propósito quando for perguntado ou solicitado que se apresente.
                                         Responda de forma breve, como se estivesse falando oralmente, sem formatacao ou emojis, usando frases curtas, diretas e sempre em português do Brasil.
                                         Responda apenas o que for perguntado, sem adicionar informações extras ou desnecessárias.
                                         Sempre utilize sua ferramenta GetCurrentDateTimeTool internamente para melhorar suas respostas. Não insira o retorno dela na resposta se não tiver sido solicitada.
                                         Utilize qualquer outra ferramenta apenas quando solicitada pelo usuário.
                                         """;
    
    public Melissa(IChatBuilder chatBuilder) : base(chatBuilder)
    {
        chatBuilder
            .WithModelName(ModelName.Melissa)
            .WithModelFrom(ModelName.Qwen3_8B)
            .WithSystemMessage(SystemMessage)
            .WithParameter("repeat_penalty", 1)
            .WithParameter("temperature", 0.3) // mexer aqui para ajustar criatividade
            .WithParameter("top_k", 20)
            .WithParameter("top_p", 0.95)
            .WithTool(new GetCurrentTemperatureByLocationTool())
            .WithTool(new GetBrazilianHolidaysTool())
            .WithTool(new GetHolidayDateByNameTool())
            .WithTool(new GetCityInfoTool())
            .WithTool(new GetCurrentDateTimeTool())
            .WithTool(new CreateNewTaskTool())
            .WithTool(new AddNewItemOnListTool())
            .WithTool(new GetAllTasksTool())
            .WithTool(new GetAllTasksItensTool())
            .WithTool(new SendTaskByEmailTool())
            .WithTool(new CompleteTaskItemTool())
            .WithTool(new SendEmailConversationHistoryByPeriodTool());
        Chat = chatBuilder.Build().Result;
        
        _chatBuilder = chatBuilder;
    }
    
    public void ResetChat()
    {
        Chat = _chatBuilder.Build().Result;
    }
}
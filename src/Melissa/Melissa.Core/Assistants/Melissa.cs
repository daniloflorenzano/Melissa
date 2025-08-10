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
    
    public Melissa(IChatBuilder chatBuilder) : base(chatBuilder)
    {
        chatBuilder
            .WithModelName(ModelName.Melissa)
            .WithTool(new GetCurrentTemperatureByLocationTool())
            .WithTool(new GetBrazilianHolidaysTool())
            .WithTool(new GetHolidayDateByNameTool())
            .WithTool(new GetCityInfoTool())
            .WithTool(new GetCurrentDateTimeTool())
            .WithTool(new CreateNewTaskTool())
            .WithTool(new AddNewItemOnListTool())
            .WithTool(new GetAllTasksTool())
            .WithTool(new SendEmailConversationHistoryByPeriodTool());
        Chat = chatBuilder.Build().Result;
        
        _chatBuilder = chatBuilder;
    }
    
    public void ResetChat()
    {
        Chat = _chatBuilder.Build().Result;
    }
}
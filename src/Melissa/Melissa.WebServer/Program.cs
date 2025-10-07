using Melissa.Core.AiTools.Localization;
using Melissa.Core.Assistants;
using Melissa.Core.ExternalData;
using Melissa.WebServer;
using Serilog;
using FFMpegCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options =>
{
    options.DisableImplicitFromServicesParameters = true;
    options.MaximumReceiveMessageSize = 50 * 1024 * 1024;
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var assistantFactory = new AssistantFactory();
var melissa = await assistantFactory.TryCreateMelissa(TimeSpan.FromSeconds(10));

// A assistente precisa ser um Singleton para ser persistido o contexto da conversa
builder.Services.AddSingleton(melissa);

GlobalFFOptions.Configure(new FFOptions
{
    TemporaryFilesFolder = Path.GetTempPath()
});

var allUNeedApiBaseAddress = builder.Configuration.GetValue<string>("AllUNeedApiUrl");
if (string.IsNullOrEmpty(allUNeedApiBaseAddress))
    throw new InvalidOperationException("AllUNeedApiBaseAddress não está configurado.");

var allUNeedApiKey = builder.Configuration.GetValue<string>("AllUNeedApiKey");

var allUNeedApiOptions = AllUNeedApiOptions.GetInstance();
allUNeedApiOptions.BaseAddress = allUNeedApiBaseAddress;
allUNeedApiOptions.ApiKey = allUNeedApiKey ?? string.Empty;

var app = builder.Build();

// TODO: pensar em como fazer quando tornar a aplicação em uma imagem docker
var holidaysCsvPath = Path.Combine(
    PathUtils.TryGetSolutionDirectoryInfo().Parent!.Parent!.FullName,
    "data",
    app.Configuration.GetValue<string>("HolidaysCsvName")!
);

await DatabaseFeeder.FeedHolidays(holidaysCsvPath);

app.MapHub<MelissaHub>("/melissa");

app.MapPost("/melissa/AskMelissaAudio", AudioEndpoints.AskMelissaAudio);

// Rotas de ferramentas
app.MapGet("/melissa/GetCurrentTemperatureByLocation",  async (string location) => await AppEndpoints.GetCurrentWeatherByLocalizationAsync(location));
app.MapGet("/melissa/ExportNationalHolidaysToTxt", AppEndpoints.ExportNationalHolidaysToTxt);

#region Tarefas

// AddNewTask
app.MapPost("/melissa/AddNewTask", AppEndpoints.AddNewTask);

// AddNewItenTask
app.MapPost("/melissa/AddNewItenTask", AppEndpoints.AddNewItemTask);

// CancelTaskItemById
app.MapPost("/melissa/CancelTaskItemById", AppEndpoints.CancelTaskItemById);

// GetAllTasks
app.MapGet("/melissa/GetAllTasks", AppEndpoints.GetAllTasks);

// GetAllItensTasks
app.MapGet("/melissa/GetAllItensTasks", AppEndpoints.GetAllItensByTaskId);

// CompleteItenTask
app.MapPost("/melissa/CompleteItemTask", AppEndpoints.CompleteItemTask);

// SendTaskByEmail
app.MapPost("/melissa/SendTaskByEmail", AppEndpoints.SendTaskByEmail);

app.MapPost("/melissa/ArchiveTaskById", AppEndpoints.ArchiveTaskById);

app.MapPost("/melissa/UnarchiveTaskById", AppEndpoints.UnarchiveTaskById);

#endregion

app.MapPost("/melissa/SendEmailConversationHistoryByPeriod", AppEndpoints.SendEmailConversationHistoryByPeriod);

app.MapGet("/health",  async context =>
{
    var melissaStatus = await melissa.CanUse();
    if (melissaStatus.isAvailable)
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("OK");
        return;
    }
    
    context.Response.StatusCode = 500;
    await context.Response.WriteAsync(melissaStatus.statusMessage);
});

app.Run();
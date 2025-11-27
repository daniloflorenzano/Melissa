using Melissa.Core.AiTools.Localization;
using Melissa.Core.Assistants;
using Melissa.Core.ExternalData;
using Melissa.WebServer;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options => { options.DisableImplicitFromServicesParameters = true; });

// Registrar DbContext no DI container
builder.Services.AddDbContext<AppDbContext>();

// Adicionar configuração de CORS para permitir requisições do app mobile
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Permite qualquer origem
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Necessário para SignalR
    });
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var assistantFactory = new AssistantFactory();
var melissa = await assistantFactory.TryCreateMelissa(TimeSpan.FromSeconds(10));

await MelissaHub.DownloadModel(MelissaHub.ModelFileName, MelissaHub.GgmlType);

// A assistente precisa ser um Singleton para ser persistido o contexto da conversa
builder.Services.AddSingleton(melissa);

var allUNeedApiBaseAddress = builder.Configuration.GetValue<string>("AllUNeedApiUrl");
var allUNeedApiKey = builder.Configuration.GetValue<string>("AllUNeedApiKey");

const string someFunctionalityMayNotWorkWarning = "Algumas funcionalidades podem não funcionar corretamente.";

if (string.IsNullOrEmpty(allUNeedApiBaseAddress))
    Log.Warning("AllUNeedApiBaseAddress não está configurado. {SomeFunctionalityMayNotWorkWarning}", someFunctionalityMayNotWorkWarning);
else if (string.IsNullOrEmpty(allUNeedApiKey))
    Log.Warning("AllUNeedApiKey não está configurado. {SomeFunctionalityMayNotWorkWarning}", someFunctionalityMayNotWorkWarning);

var allUNeedApiOptions = AllUNeedApiOptions.GetInstance();
allUNeedApiOptions.BaseAddress = allUNeedApiBaseAddress ?? string.Empty;
allUNeedApiOptions.ApiKey = allUNeedApiKey ?? string.Empty;

var app = builder.Build();

// Aplicar migrations automaticamente no startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Log.Information("Aplicando migrations do banco de dados...");
        dbContext.Database.Migrate();
        Log.Information("Migrations aplicadas com sucesso!");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro ao aplicar migrations do banco de dados.");
    }
}

// Habilitar CORS
app.UseCors();

var holidaysCsvSetting = app.Configuration.GetValue<string>("HolidaysCsvPath") ?? "data/holidays_2025.csv";

string holidaysCsvPath;
if (Path.IsPathRooted(holidaysCsvSetting))
{
    holidaysCsvPath = holidaysCsvSetting;
}
else
{
    var normalized = holidaysCsvSetting.Replace('/', Path.DirectorySeparatorChar).Replace("\\", Path.DirectorySeparatorChar.ToString());
    holidaysCsvPath = Path.Combine(app.Environment.ContentRootPath, normalized);
}

if (!File.Exists(holidaysCsvPath))
    Log.Warning("Arquivo de feriados não encontrado: {Path}. {SomeFunctionalityMayNotWorkWarning}", holidaysCsvPath,
        someFunctionalityMayNotWorkWarning);
else
    await DatabaseFeeder.FeedHolidays(holidaysCsvPath);

app.MapHub<MelissaHub>("/melissa");

app.MapPost("/melissa/AskMelissaAudio", AudioEndpoints.AskMelissaAudio);

// Rotas de ferramentas
app.MapGet("/melissa/GetCurrentTemperatureByLocation",  async (string location) => await AppEndpoints.GetCurrentWeatherByLocalizationAsync(location));
app.MapGet("/melissa/ExportNationalHolidaysToTxt", async () => await AppEndpoints.GetNationalHolidaysAsTxt());

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
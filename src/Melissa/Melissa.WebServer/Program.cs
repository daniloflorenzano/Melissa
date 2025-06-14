using Melissa.Core.Assistants;
using Melissa.Core.ExternalData;
using Melissa.WebServer;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using MelissaAssistant = Melissa.Core.Assistants.Melissa;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options => { options.DisableImplicitFromServicesParameters = true; });

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var assistantFactory = new AssistantFactory();
var melissa = await assistantFactory.TryCreateMelissa(TimeSpan.FromSeconds(10));

// A assistente precisa ser um Singleton para ser persistido o contexto da conversa
builder.Services.AddSingleton(melissa);

var app = builder.Build();

// TODO: pensar em como fazer quando tornar a aplicação em uma imagem docker
var holidaysCsvPath = Path.Combine(
    PathUtils.TryGetSolutionDirectoryInfo().Parent!.Parent!.FullName,
    "data",
    app.Configuration.GetValue<string>("HolidaysCsvName")!
);

await DatabaseFeeder.FeedHolidays(holidaysCsvPath);

//app.MapHub<MelissaHub>("/melissa");

app.MapPost("/melissa/AskMelissaAudio", AudioEndpoints.AskMelissaAudio);


app.MapGet("/melissa", async ([FromServices] MelissaAssistant melissaAssistant) =>
{
    var (isAvailable, statusMessage) = await melissaAssistant.CanUse();
    statusMessage = string.IsNullOrEmpty(statusMessage) ? "Assistente pronta" : statusMessage;
    
    return Results.Ok(new
    {
        available = isAvailable,
        message = statusMessage
    });
});

app.Run();
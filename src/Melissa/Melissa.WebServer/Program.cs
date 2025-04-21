using Melissa.Core.Chats.Ollama;
using Melissa.Core.ExternalData;
using Melissa.WebServer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options => { options.DisableImplicitFromServicesParameters = true; });

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var melissa = new Melissa.Core.Assistants.Melissa(new OllamaChatBuilder());
builder.Services.AddSingleton(melissa);

var app = builder.Build();

// TODO: pensar em como fazer quando tornar a aplicação em uma imagem docker
var holidaysCsvPath = Path.Combine(
    PathUtils.TryGetSolutionDirectoryInfo().Parent!.Parent!.FullName,
    "data",
    app.Configuration.GetValue<string>("HolidaysCsvName")!
);

await DatabaseFeeder.FeedHolidays(holidaysCsvPath);

app.MapHub<MelissaHub>("/melissa");
app.Run();
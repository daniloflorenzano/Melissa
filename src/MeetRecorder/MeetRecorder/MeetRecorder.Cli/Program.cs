using System.Diagnostics;
using MeetRecorder.Cli;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(
        path: "logs/meetrecorder.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7
    )
    .CreateLogger();

var timeout = TimeSpan.FromSeconds(30);
var cancellationTokenSource = new CancellationTokenSource(timeout);

Log.Information("Aplicação iniciada");
var host = new NativeMessagingHost();
try
{
    await Task.WhenAny(
        host.RunAsync(),
        Task.Delay(Timeout.Infinite, cancellationTokenSource.Token)
    );
    
    if (cancellationTokenSource.IsCancellationRequested) 
    {
        try
        {
            host.Dispose();
            Log.Warning("Timeout de {Timeout} segundos atingido", timeout.TotalSeconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao encerrar o host");
        }
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal");
}
finally
{
    host.Dispose();   
    Log.Information("Aplicação encerrada");
    Log.CloseAndFlush();
}

using System.Net.WebSockets;
using System.Text;
using Melissa.Core.Assistants;
using Melissa.Core.Chats.Ollama;
using Melissa.Core.ExternalData;
using Melissa.WebServer;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// TODO: pensar em como fazer quando tornar a aplicação em uma imagem docker
var holidaysCsvPath = Path.Combine(
    PathUtils.TryGetSolutionDirectoryInfo().Parent!.Parent!.FullName,
    "data",
    app.Configuration.GetValue<string>("HolidaysCsvName")!
);

await DatabaseFeeder.FeedHolidays(holidaysCsvPath);

var websocketOptions = new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
};

app.UseWebSockets(websocketOptions);


app.Use(async (context, next) =>
{
    if (context.Request.Path == "/askmelissa")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var melissa = new Melissa.Core.Assistants.Melissa(new OllamaChatBuilder());
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await TalkToMelissaNew(webSocket, melissa);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();
return;

static async Task TalkToMelissaNew(WebSocket socket, Assistant assistant)
{
    var buffer = new byte[1024 * 4];
    var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    while (!receiveResult.CloseStatus.HasValue)
    {
        var userText = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        var question = new Question(userText, "WSClient", DateTimeOffset.Now);

        if (await assistant.CanUse())
        {
            await foreach (var answerToken in assistant.Ask(question))
            {
                var response = Encoding.UTF8.GetBytes(answerToken);
                await socket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, false,
                    CancellationToken.None);
            }

            // avisa fim da mensagem
            await socket.SendAsync(new ArraySegment<byte>("\n"u8.ToArray()), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        else
        {
            var response = "Melissa: Não posso responder perguntas agora."u8.ToArray();
            await socket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }

        receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    await socket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription,
        CancellationToken.None);
}
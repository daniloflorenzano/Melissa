using System.Net.WebSockets;
using System.Text;

var ws = new ClientWebSocket();
await ws.ConnectAsync(new Uri("ws://localhost:5179/askmelissa"), CancellationToken.None);

var recieveTask = Task.Run(async () =>
{
    var buffer = new byte[1024];
    while (true)
    {
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                
        if (result.MessageType == WebSocketMessageType.Close)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            break;
        }
                
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.Write(message);
    }

});
        
var sendTask = Task.Run(async () =>
{
    while (true)
    {
        var message = Console.ReadLine();
        if (message == "exit")
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            break;
        }

        if (string.IsNullOrWhiteSpace(message)) 
            continue;
        
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }
});

await Task.WhenAny(recieveTask, sendTask);
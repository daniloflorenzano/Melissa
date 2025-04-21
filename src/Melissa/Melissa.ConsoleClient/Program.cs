using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5179/melissa")
    .Build();

await connection.StartAsync();

var cancellationToken = new CancellationTokenSource();

connection.Closed += async (error) =>
{
    await Task.Delay(new Random().Next(0, 5) * 1000);
    await connection.StartAsync();
};


while (true)
{
    var message = Console.ReadLine();

    if (message == "exit")
    {
        await connection.StopAsync();
        break;
    }

    var stream = connection.StreamAsync<string>("AskMelissaText", message, cancellationToken: cancellationToken.Token);
    await foreach (var word in stream)
    {
        Console.Write(word);
    }
    Console.WriteLine();
}
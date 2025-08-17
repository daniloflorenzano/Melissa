using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Melissa.Core.ExternalData;
using Melissa.WebServer.Email;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using Serilog;

namespace Melissa.WebServer;

public class ConversationHistoryOllamaTool
{
    /// <summary>
    /// Envia por e-mail o histórico de conversas de um determinado período (Hora, Dia ou Mês).
    /// </summary>
    /// <param name="period">Período das conversas. Pode ser as conversas da última hora, último dia ou último mês</param>
    [OllamaTool]
    public static async Task<string> SendEmailConversationHistoryByPeriod(string period)
    {
        Log.Information("Executando a ferramenta SendEmailConversationHistoryByPeriod com o período: {Period}", period);
        
        ConversationHistoryService conversationHistoryService = new ConversationHistoryService();
        await using var context = new AppDbContext();

        DateTime dataMinima;

        if (period.Contains("hora") || period.Contains("hour"))
            dataMinima = DateTime.Now.AddHours(-1);
        else if (period.Contains("dia") || period.Contains("day"))
            dataMinima = DateTime.Now.AddDays(-1);
        else if (period.Contains("mês") || period.Contains("month"))
            dataMinima = DateTime.Now.AddMonths(-1);
        else
            return "Período inválido. Use 'hora', 'dia' ou 'mes'.";

        var historyList = await context.DbHistoryData
            .Where(h => h.Data >= dataMinima)
            .OrderByDescending(h => h.Data)
            .ToListAsync();
        
        conversationHistoryService.SendConversationHistory(historyList);
        
        return "Histórico de conversa enviado com sucesso";
    }
}
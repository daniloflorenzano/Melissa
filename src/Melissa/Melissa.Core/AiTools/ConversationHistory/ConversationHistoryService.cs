using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Melissa.Core.ExternalData;
using Melissa.WebServer.Email;
using Microsoft.EntityFrameworkCore;

namespace Melissa.WebServer;

public class ConversationHistoryService
{
    public void SendConversationHistory(List<DbConversationHistory> historyList, string? email = null)
    {
        var config = ReadEmailConfig();

        if (email != null)
            config.Destinatarios = [email];

        var assunto = "Histórico de conversa - Melissa";

        var corpoBuilder = new StringBuilder();
        foreach (var item in historyList)
        {
            corpoBuilder.AppendLine($"Pergunta: {item.Pergunta}");
            corpoBuilder.AppendLine($"Resposta: {item.Resposta}");
            corpoBuilder.AppendLine($"Data: {item.Data:dd/MM/yyyy HH:mm}");
            corpoBuilder.AppendLine();
        }

        var corpo = corpoBuilder.ToString();

        var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            Credentials = new NetworkCredential(config.Credenciais.UserName, config.Credenciais.Password),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(config.Credenciais.UserName, "Melissa"),
            Subject = assunto,
            Body = corpo,
            IsBodyHtml = false,
        };

        foreach (var dest in config.Destinatarios)
        {
            mailMessage.To.Add(dest);
        }

        smtpClient.Send(mailMessage);
    }
    
    public static EmailConfig ReadEmailConfig()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var pathConfig = Path.Join(path, "EmailMelissaConfig.json");
        
        var json = File.ReadAllText(pathConfig);
        return JsonSerializer.Deserialize<EmailConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
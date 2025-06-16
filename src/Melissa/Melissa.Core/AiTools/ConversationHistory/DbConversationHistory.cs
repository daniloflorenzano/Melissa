using System.ComponentModel.DataAnnotations;

namespace Melissa.WebServer;

public class DbConversationHistory
{
    [Key]
    public int Id { get; set; }
    public string Pergunta { get; set; }
    public string Resposta { get; set; }
    public DateTime Data { get; set; }
}
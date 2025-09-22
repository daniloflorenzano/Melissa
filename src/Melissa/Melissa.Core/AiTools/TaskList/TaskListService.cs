using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Melissa.Core.ExternalData;
using Melissa.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Core.AiTools.TaskList;

public class TaskListService
{
    private readonly AppDbContext _dbContext = new();

    /// <summary>
    /// Registra uma nova tarefa.
    /// </summary>
    public async Task RegisterNewTask(Tasks task)
    {
        try
        {
            await _dbContext.Tasks.AddAsync(task);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Adiciona um novo Item à uma determinada Task
    /// </summary>
    /// <param name="taskItem"></param>
    /// <param name="taskTitle"></param>
    public async Task AddNewTaskItem(TaskItens taskItem, string taskTitle)
    {
        var task = await _dbContext.Tasks.Where(t => t.Title.Contains(taskTitle)).ToListAsync();
        taskItem.TaskId = task.Select(t => t.Id).Max();

        try
        {
            await _dbContext.TaskItens.AddAsync(taskItem);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Adiciona um novo Item à uma determinada Task
    /// </summary>
    /// <param name="taskItem"></param>
    /// <param name="taskTitle"></param>
    public async Task AddNewTaskItemByTaskId(TaskItens taskItem)
    {
        try
        {
            await _dbContext.TaskItens.AddAsync(taskItem);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Lista todas as tarefas registradas.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Tasks>> GetAllTasks()
    {
        try
        {
            return await _dbContext.Tasks.ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Lista todas as tarefas registradas.
    /// </summary>
    /// <returns></returns>
    public async Task<Tasks> GetTaskByName(string taskName)
    {
        Tasks taskResult = new Tasks();

        try
        {
            var task = await _dbContext.Tasks.Where(c => c.Title == taskName).ToListAsync();

            if (task.Any())
            {
                taskResult = new Tasks()
                {
                    Id = task.Select(t => t.Id).Max(),
                    Title = task.Select(t => t.Title).Max(),
                    Description = task.Select(t => t.Description).Max(),
                    IncludedAt = task.Select(t => t.IncludedAt).Max()
                };
            }

            return taskResult;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Lista todas as tarefas registradas.
    /// </summary>
    /// <returns></returns>
    public async Task<Tasks> GetTaskById(int taskId)
    {
        Tasks taskResult = new Tasks();

        try
        {
            var task = await _dbContext.Tasks.Where(c => c.Id == taskId).ToListAsync();

            if (task.Any())
            {
                return new Tasks()
                {
                    Id = task.Select(t => t.Id).Max(),
                    Title = task.Select(t => t.Title).Max(),
                    Description = task.Select(t => t.Description).Max(),
                    IncludedAt = task.Select(t => t.IncludedAt).Max()
                };
            }

            return taskResult;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Lista os itens de uma tarefa específica pelo ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<List<TaskItens>> GetTaskItensByTaskId(int id)
    {
        try
        {
            return await _dbContext.TaskItens.Where(t => t.TaskId == id).ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// Completa um item de tarefa específico pelo ID.
    /// </summary>
    /// <param name="idItem"></param>
    public async Task<string> UpdateCompleteStatusTaskItem(int idItem)
    {
        try
        {
            var rows = await _dbContext.TaskItens
                .Where(t => t.Id == idItem)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsCompleted, t => !t.IsCompleted)
                    .SetProperty(t => t.CompletedAt, t => t.IsCompleted
                        ? (DateTime?)null              // estava concluída -> limpa
                        : DateTime.UtcNow));           // não estava -> conclui agora

            // Sincroniza o contexto para não ver valores antigos
            _dbContext.ChangeTracker.Clear();

            // (Opcional) buscar o estado atualizado sem tracking
            var updated = await _dbContext.TaskItens
                .AsNoTracking()
                .Where(t => t.Id == idItem)
                .Select(t => new { t.IsCompleted, t.CompletedAt })
                .SingleOrDefaultAsync();

            if (rows == 0 || updated is null)
                return $"Nenhum item encontrado com ID {idItem}.";

            var msg = updated.IsCompleted
                ? $"Item {idItem} marcado como concluído em {updated.CompletedAt}."
                : $"Item {idItem} voltou para pendente.";

            return msg;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return "Erro ao atualizar o status do item.";
        }
    }
    
    /// <summary>
    /// Envia por e-mail uma lista de itens pertencentes a uma determinada tarefa.
    /// </summary>
    /// <param name="email">E-mail do destinatário.</param>
    /// <param name="itens">Lista de itens (todos do mesmo TaskId).</param>
    /// <param name="nomeTarefa">Nome/título da tarefa.</param>
    public async Task SendTaskByEmailAsync(string email, List<TaskItens> itens, string nomeTarefa)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail inválido.", nameof(email));

        if (itens == null || itens.Count == 0)
            throw new ArgumentException("A lista de itens está vazia.", nameof(itens));
        
        var taskId = itens.First().TaskId;
        
        var culture = new CultureInfo("pt-BR");

        var subject = $"Tarefa #{taskId} - {nomeTarefa} ({itens.Count} item(ns))";

        // Monta corpo em HTML com uma tabela
        var sb = new StringBuilder();
        sb.Append($@"
                    <!DOCTYPE html>
                    <html lang='pt-BR'>
                    <head>
                    <meta charset='UTF-8'>
                    <style>
                      body {{ font-family: Arial, Helvetica, sans-serif; }}
                      .wrap {{ max-width: 720px; margin: 0 auto; }}
                      h1 {{ font-size: 18px; }}
                      table {{ border-collapse: collapse; width: 100%; }}
                      th, td {{ border: 1px solid #ddd; padding: 8px; font-size: 14px; }}
                      th {{ background: #f5f5f5; text-align: left; }}
                      .ok {{ color: #0a7; font-weight: bold; }}
                      .pendente {{ color: #b50; font-weight: bold; }}
                    </style>
                    </head>
                    <body>
                    <div class='wrap'>
                      <h1>Tarefa #{taskId} — {WebUtility.HtmlEncode(nomeTarefa)}</h1>
                      <p>Segue a lista de itens ({itens.Count}):</p>
                      <table>
                        <thead>
                          <tr>
                            <th>ID</th>
                            <th>Descrição</th>
                            <th>Incluída em</th>
                            <th>Concluída em</th>
                            <th>Status</th>
                          </tr>
                        </thead>
                        <tbody>");

        foreach (var item in itens.OrderBy(i => i.IncludedAt))
        {
            var included = item.IncludedAt.ToString("dd/MM/yyyy HH:mm", culture);
            var completed = (item.IsCompleted && item.CompletedAt != default)
                ? item.CompletedAt?.ToString("dd/MM/yyyy HH:mm", culture)
                : "-";

            var statusText = item.IsCompleted ? "Concluído" : "Pendente";
            var statusClass = item.IsCompleted ? "ok" : "pendente";

            sb.Append($@"
                         <tr>
                           <td>{item.Id}</td>
                           <td>{WebUtility.HtmlEncode(item.Description)}</td>
                           <td>{included}</td>
                           <td>{completed}</td>
                           <td class='{statusClass}'>{statusText}</td>
                         </tr>");
        }

        sb.Append(@"
                        </tbody>
                      </table>
                      <p style='font-size:12px;color:#666'>Mensagem automática — Melissa</p>
                    </div>
                    </body>
                    </html>");

        var bodyHtml = sb.ToString();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress("joao.jordao@aedb.br", "Melissa"),
            Subject = subject,
            Body = bodyHtml,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        mailMessage.To.Add(new MailAddress(email));
        
        var (appEmail, appPass) = Credentials.EmailCredsLoader.Load();

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(appEmail, appPass),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await smtp.SendMailAsync(mailMessage);
    }
    
    /// <summary>
    /// Cancela (marca como cancelado) um item de tarefa específico pelo ID.
    /// </summary>
    /// <param name="taskItenId"></param>
    public async Task CancelTaskItemById(int taskItenId, int taskId)
    {
        try
        {
            var rows = await _dbContext.TaskItens
                .Where(t => t.Id == taskItenId && t.TaskId == taskId)
                .ExecuteUpdateAsync(t => t
                    .SetProperty(i => i.CanceledAt, DateTime.Now)
                    .SetProperty(i => i.IsCanceled, true));

            await _dbContext.SaveChangesAsync();

            if (rows == 0)
                Console.WriteLine($"Nenhum item encontrado para o item ID {taskItenId}.");
            else
                Console.WriteLine($"{rows} item foi cancelado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}
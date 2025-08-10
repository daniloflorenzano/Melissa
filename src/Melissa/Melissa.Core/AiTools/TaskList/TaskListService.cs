using System.Net.Mail;
using Melissa.Core.ExternalData;
using Microsoft.EntityFrameworkCore;

namespace Melissa.Core.AiTools.TaskList;

public class TaskListService
{
    private readonly AppDbContext _dbContext = new();
    
    /// <summary>
    /// Registra uma nova tarefa.
    /// </summary>
    public async Task RegisterNewTask( Tasks task )
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
    
    public async Task AddNewTaskItem( TaskItens taskItem, string taskTitle )
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
    /// Envia 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="task"></param>
    public async Task SendTaskByEmail(string email, TaskItens task)
    {
        try
        {
            var emailService = new SmtpClient();

            #region SmtpClient Configuration

            emailService.Host = "smtp.gmail.com";
            emailService.Port = 587;
            emailService.EnableSsl = true;
            emailService.Credentials = new System.Net.NetworkCredential("...", "...");

            #endregion

            #region Email Configuration
            
            var mailMessage = new MailMessage
            {
                From = new MailAddress("..."),
                Subject = "Nova Tarefa Registrada",
                Body = $"Tarefa: {task.TaskId}\nDescrição: {task.Description}\nIncluída em: {task.IncludedAt}"
            };
            
            #endregion
            
            mailMessage.To.Add(new MailAddress(email));
            await emailService.SendMailAsync(mailMessage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
  
}
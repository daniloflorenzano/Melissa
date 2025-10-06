using DefaultNamespace;
using Melissa.Core.AiTools.Holidays;
using Melissa.Core.AiTools.TaskList;
using Melissa.Core.ExternalData;
using Microsoft.EntityFrameworkCore;
using Melissa.Core.ExternalData;

namespace Melissa.WebServer;

public class AppEndpoints
{
    static TaskListService TaskListService = new TaskListService();

    /// <summary>
    /// Retorna a temperatura atual de uma localização específica.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static async Task<string> GetCurrentWeatherByLocalizationAsync(string location)
    {
        var weatherService = new WeatherService();

        var weather = await weatherService.GetWeatherAsync(location);
        var tempAtual = weather.TemperaturaAtual;

        return tempAtual;
    }

    /// <summary>
    /// Exporta os feriados nacionais para um arquivo txt.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static async Task ExportNationalHolidaysToTxt()
    {
        var holidayService = new HolidayService();
        await holidayService.ExportNationalHolidaysToTxt();
    }

    #region TaskList

    /// <summary>
    /// Adiciona uma nova tarefa à lista de tarefas.
    /// </summary>
    /// <param name="taskTitle"></param>
    public static async Task AddNewTask(string taskTitle, string? taskDescription = null)
    {
        var task = new Tasks();

        task.Title = taskTitle;
        task.Description = taskDescription ?? "Nova Lista";
        task.IncludedAt = DateTime.Now;

        await TaskListService.RegisterNewTask(task);
    }

    /// <summary>
    /// Adiciona um novo Itens à uma tarefa específica.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="taskDescription"></param>
    public static async Task AddNewItemTask(int taskId, string taskDescription)
    {
        var taskItem = new TaskItens();

        taskItem.TaskId = taskId;
        taskItem.Description = taskDescription;
        taskItem.IncludedAt = DateTime.Now;
        taskItem.IsCompleted = false;

        await TaskListService.AddNewTaskItemByTaskId(taskItem);
    }

    // Cancela um item de uma tarefa.
    public static async Task CancelTaskItemById(int taskItenId, int taskId)
    {
        await TaskListService.CancelTaskItemById(taskItenId, taskId);
    }

    /// <summary>
    /// Busca todas as tarefas na lista de tarefas.
    /// </summary>
    /// <returns></returns>
    public static async Task<List<Tasks>> GetAllTasks()
    {
        return await TaskListService.GetAllTasks();
    }

    /// <summary>
    /// Busca todos os itens de uma Tarefa
    /// </summary>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public static async Task<List<TaskItens>> GetAllItensByTaskId(int taskId)
    {
        return await TaskListService.GetTaskItensByTaskId(taskId);
    }

    /// <summary>
    /// Completa um item de tarefa.
    /// </summary>
    /// <param name="taskItenId"></param>
    public static async Task CompleteItemTask(int taskItenId)
    {
        await TaskListService.UpdateCompleteStatusTaskItem(taskItenId);
    }

    /// <summary>
    /// Envia por e-mail os itens de uma tarefa específica.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="taskId"></param>
    /// <param name="taskName"></param>
    public static async Task SendTaskByEmail(string email, int taskId)
    {
        var taskServive = new TaskListService();
        
        var taskDetails = await taskServive.GetTaskById(taskId);

        List<TaskItens> taskItems = await taskServive.GetTaskItensByTaskId(taskId);

        if (taskItems.Count > 0)
            await taskServive.SendTaskByEmailAsync(email, taskItems, taskDetails.Title);
    }

    /// <summary>
    /// Arquiva uma tarefa específica.
    /// </summary>
    /// <param name="taskId"></param>
    public static async Task ArchiveTaskById(int taskId)
    {
        await TaskListService.ArchiveTaskById(taskId);
    }

    /// <summary>
    /// Desarquiva uma tarefa específica.
    /// </summary>
    /// <param name="taskId"></param>
    public static async Task UnarchiveTaskById(int taskId)
    {
        await TaskListService.UnarchiveTaskById(taskId);
    }

    #endregion

    public static async Task SendEmailConversationHistoryByPeriod(string email, DateTime startPeriod,
        DateTime endPeriod)
    {
        await using var context = new AppDbContext();

        var historyList = await context.DbHistoryData
            .Where(h => h.Data >= startPeriod && h.Data <= endPeriod)
            .OrderByDescending(h => h.Data)
            .ToListAsync();
        
        ConversationHistoryService conversationHistoryService = new ConversationHistoryService();
        conversationHistoryService.SendConversationHistory(historyList, email);
    }
}
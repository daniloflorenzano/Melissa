using System.Text;
using OllamaSharp;

namespace Melissa.Core.AiTools.TaskList;

public class TaskListOllamaTools
{
    /// <summary>
    /// Cria uma nova tarefa com o título e descrição fornecidos.
    /// </summary>
    /// <param name="taskTitle">Título da Tarefa</param>
    /// <param name="taskDescription">Descrição da Tarefa</param>
    /// <returns>Retorna o nome da lista criada</returns>
    [OllamaTool]
    public static async Task<string> CreateNewTask(string taskTitle)
    {
        var taskServive = new TaskListService();
        var task = new Tasks();

        task.Title = taskTitle;
        task.Description = "Nova Lista";
        task.IncludedAt = DateTime.Now;

        await taskServive.RegisterNewTask(task);
        return $"Tarefa '{taskTitle}' criada com sucesso!";
    }
    
    /// <summary>
    /// Adiciona um novo item em uma lista de tarefas já existente.
    /// </summary>
    /// <param name="taskDescription"></param>
    /// <param name="taskTitle"></param>
    /// <returns>Retorna item adicionado</returns>
    [OllamaTool]
    public static async Task<string> AddNewItemOnList(string taskDescription, string taskTitle)
    {
        var taskServive = new TaskListService();
        var taskItem = new TaskItens();

        taskItem.Description = taskDescription;
        taskItem.IncludedAt = DateTime.Now;
        taskItem.IsCompleted = false;

        await taskServive.AddNewTaskItem(taskItem, taskTitle);
        return $"Novo Item - {taskDescription} - adicionado na Lista '{taskTitle}'";
    }
    
    /// <summary>
    /// Lista todas as tarefas criadas, incluindo título, descrição e data de criação.
    /// </summary>
    /// <returns>Retorna as informações de cada lista existente</returns>
    [OllamaTool]
    public static async Task<string> GetAllTasks()
    {
        var taskServive = new TaskListService();
        
        List<Tasks> listTasks = await taskServive.GetAllTasks();
        
        if (listTasks == null || listTasks.Count == 0)
        {
            return "Nenhuma tarefa encontrada.";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Lista de tarefas:");
        sb.AppendLine("-----------------");

        foreach (var task in listTasks)
        {
            sb.AppendLine($"ID: {task.Id}");
            sb.AppendLine($"Título: {task.Title}");
            sb.AppendLine($"Descrição: {task.Description}");
            sb.AppendLine($"Data de criação: {task.IncludedAt:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// Lista os itens de uma tarefa específica.
    /// </summary>
    /// <param name="taskName"></param>
    /// <returns>Retorna todos os itens de uma Tarefa</returns>
    [OllamaTool]
    public static async Task<string> GetAllTasksItens(string taskName)
    {
        var taskServive = new TaskListService();
        
        Tasks task = await taskServive.GetTaskByName(taskName);
        
        if (task.Id == 0)
            return $"Nenhum item encontrado para a Tarefa {taskName}.";
        
        List<TaskItens> taskItems = await taskServive.GetTaskItensByTaskId(task.Id);
        
        var sb = new StringBuilder();
        sb.AppendLine("Itens:");
        sb.AppendLine("-----------------");

        foreach (var item in taskItems)
        {
            sb.AppendLine($"Descrição: {item.Description}");
            sb.AppendLine($"Data de criação: {item.IncludedAt:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Completado em: {(item.IsCompleted ? item.CompletedAt.ToString("dd/MM/yyyy HH:mm") : "Não completado")}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
    
    /// <summary>
    /// Atualiza o status de um item de tarefa para "completado".
    /// </summary>
    /// <param name="taskName"></param>
    /// <param name="itemName"></param>
    /// <returns></returns>
    [OllamaTool]
    public static async Task<string> CompleteTaskItem(string taskName, string itemName)
    {
        var taskServive = new TaskListService();
        
        Tasks task = await taskServive.GetTaskByName(taskName);
        
        if (task.Id == 0)
            return $"Nenhum item encontrado para a Tarefa {taskName}.";
        
        List<TaskItens> taskItems = await taskServive.GetTaskItensByTaskId(task.Id);
        var item = taskItems.FirstOrDefault(i => i.Description.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        
        if (item == null)
            return $"Item '{itemName}' não encontrado na Tarefa '{taskName}'.";

        if (item.IsCompleted)
            return $"Item '{itemName}' já está marcado como completado.";

        return await taskServive.UpdateCompleteStatusTaskItem(item.Id);
    }

    /// <summary>
    /// Envia uma lista de tarefas por email.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="taskName"></param>
    [OllamaTool]
    public static async Task<string> SendTaskByEmail(string email, string taskName)
    {
        var taskServive = new TaskListService();
        Tasks task = await taskServive.GetTaskByName(taskName);
        
        if (task.Id == 0)
            return $"Nenhum item encontrado para a Tarefa {taskName}.";
        
        List<TaskItens> taskItems = await taskServive.GetTaskItensByTaskId(task.Id);

        if (taskItems.Count > 0)
        {
            await taskServive.SendTaskByEmailAsync(email, taskItems, taskName);
            return $"Lista '{taskName}' enviada para o email {email} com sucesso!";
        }
        
        return "Não há itens para enviar.";
    }
}
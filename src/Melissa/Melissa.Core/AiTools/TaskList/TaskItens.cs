namespace Melissa.Core.AiTools.TaskList;

public class TaskItens
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string Description { get; set; }
    public DateTime IncludedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
}
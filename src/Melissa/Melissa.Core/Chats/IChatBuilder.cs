namespace Melissa.Core.Chats;

public interface IChatBuilder
{
    ModelName ModelName { get; set; }
    string SystemMessage { get; set; }
    List<object> Tools { get; }
    
    IChatBuilder AddTool(object tool);
    Task<IChat> Build();
}
namespace Melissa.Core.Chats;

public interface IChatBuilder
{
    ModelName ModelName { get; set; }
    ModelName? ModelFrom { get; set; }
    string SystemMessage { get; set; }
    Dictionary<string, object> Parameters { get; }
    IChatBuilder AddTool(object tool);
    string OllamaUrl { get; set; }
    Task<IChat> Build();
}
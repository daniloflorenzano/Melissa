namespace Melissa.Core.Chats;

public static class ChatBuilderExtensions
{
    /// <summary>
    /// Adicione a ferramenta {tool}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="tool"></param>
    /// <returns></returns>
    public static IChatBuilder WithTool(this IChatBuilder builder, object tool)
    {
        return builder.AddTool(tool);
    }
    
    /// <summary>
    /// Defina o nome do modelo como {modelName}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="modelName">LLAMA 3.2, Mistral, etc</param>
    /// <returns></returns>
    public static IChatBuilder WithModelName(this IChatBuilder builder, ModelName modelName)
    {
        builder.ModelName = modelName;
        return builder;
    }
}
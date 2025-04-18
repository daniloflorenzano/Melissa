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
    
    /// <summary>
    /// Defina o nome do assistente como {assistantName}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="assistantName"></param>
    /// <returns></returns>
    public static IChatBuilder WithAssistantName(this IChatBuilder builder, string assistantName)
    {
        builder.SystemMessage += $"Seu nome é {assistantName}.";
        return builder;
    }
    
    /// <summary>
    /// O seu objetivo é {purposeDescription}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="purposeDescription"></param>
    /// <returns></returns>
    public static IChatBuilder WithPurposeDescription(this IChatBuilder builder, string purposeDescription)
    {
        builder.SystemMessage += $"O seu objetivo é {purposeDescription}.";
        return builder;
    }
    
    /// <summary>
    /// Além disso, {adicionalDescription}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="adicionalDescription"></param>
    /// <returns></returns>
    public static IChatBuilder WithAdicionalDescription(this IChatBuilder builder, string adicionalDescription)
    {
        builder.SystemMessage += $"Além disso, {adicionalDescription}.";
        return builder;
    }
}
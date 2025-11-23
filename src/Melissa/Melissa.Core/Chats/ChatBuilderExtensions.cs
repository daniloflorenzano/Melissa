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
    /// Defina o modelo de origem como {modelFrom}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="modelFrom"></param>
    /// <returns></returns>
    public static IChatBuilder WithModelFrom(this IChatBuilder builder, ModelName modelFrom)
    {
        builder.ModelFrom = modelFrom;
        return builder;
    }
    
    /// <summary>
    /// Defina o parâmetro {key} com o valor {value}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IChatBuilder WithParameter(this IChatBuilder builder, string key, object value)
    {
        builder.Parameters[key] = value;
        return builder;
    }
    
    /// <summary>
    /// Defina a mensagem do sistema como {systemMessage}.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="systemMessage"></param>
    /// <returns></returns>
    public static IChatBuilder WithSystemMessage(this IChatBuilder builder, string systemMessage)
    {
        builder.SystemMessage = systemMessage;
        return builder;
    }
}